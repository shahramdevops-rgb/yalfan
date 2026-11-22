using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Options;
using Yalfan.Infrastructure;
using Yalfan.Infrastructure.Identity;
using Yalfan.Infrastructure.Persistence.Encryption;
using Yalfan.Infrastructure.Sms;

namespace Yalfan.Api.IntegrationTests.Auth;

/// <summary>
/// Settings that would make login unsafe stop the API from starting. Program.cs catches the
/// start failure and logs it, so the host tests only see that the API did not start; the
/// options tests check which rule refused it.
/// </summary>
public sealed class StartupGuardTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public void The_api_does_not_start_in_production_with_the_log_sms_provider()
    {
        using var production = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Production"));

        Assert.ThrowsAny<Exception>(() => production.CreateClient());
    }

    [Fact]
    public void The_api_does_not_start_with_a_short_signing_key()
    {
        using var weakKey = factory.WithWebHostBuilder(builder => builder.UseSetting("Jwt:SigningKey", "too-short"));

        Assert.ThrowsAny<Exception>(() => weakKey.CreateClient());
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public void The_log_sms_provider_is_refused_outside_development_and_testing(string environment)
    {
        using var services = Infrastructure(environment, new() { ["Sms:Provider"] = SmsProviders.Log });

        var exception = Assert.Throws<OptionsValidationException>(
            () => services.GetRequiredService<IOptions<SmsOptions>>().Value);
        Assert.Contains("only in Development and Testing", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Testing")]
    public void The_log_sms_provider_is_allowed_in_development_and_testing(string environment)
    {
        using var services = Infrastructure(environment, new() { ["Sms:Provider"] = SmsProviders.Log });

        Assert.Equal(SmsProviders.Log, services.GetRequiredService<IOptions<SmsOptions>>().Value.Provider);
    }

    [Fact]
    public void A_missing_sms_provider_is_refused()
    {
        using var services = Infrastructure("Development", []);

        var exception = Assert.Throws<OptionsValidationException>(
            () => services.GetRequiredService<IOptions<SmsOptions>>().Value);
        Assert.Contains("Sms:Provider is not set", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("0123456789012345678901234567890")]
    public void A_signing_key_shorter_than_32_bytes_is_refused(string key)
    {
        using var services = Infrastructure("Development", new() { ["Jwt:SigningKey"] = key });

        var exception = Assert.Throws<OptionsValidationException>(
            () => services.GetRequiredService<IOptions<JwtOptions>>().Value);
        Assert.Contains("Jwt:SigningKey", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void The_api_does_not_start_without_an_iban_key()
    {
        using var noKey = factory.WithWebHostBuilder(builder => builder.UseSetting("Encryption:IbanKey", string.Empty));

        Assert.ThrowsAny<Exception>(() => noKey.CreateClient());
    }

    [Theory]
    [InlineData("")]
    [InlineData("not base64 at all")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA==")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    public void An_iban_key_that_is_not_32_bytes_in_base64_is_refused(string key)
    {
        using var services = Infrastructure("Development", new() { ["Encryption:IbanKey"] = key });

        var exception = Assert.Throws<OptionsValidationException>(
            () => services.GetRequiredService<IOptions<EncryptionOptions>>().Value);
        Assert.Contains("Encryption:IbanKey", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void An_iban_key_of_32_bytes_in_base64_is_accepted()
    {
        var key = Convert.ToBase64String(new byte[EncryptionOptions.KeyBytes]);
        using var services = Infrastructure("Development", new() { ["Encryption:IbanKey"] = key });

        Assert.Equal(key, services.GetRequiredService<IOptions<EncryptionOptions>>().Value.IbanKey);
    }

    private static ServiceProvider Infrastructure(string environment, Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IHostEnvironment>(new HostingEnvironment { EnvironmentName = environment });
        services.AddInfrastructure(configuration);
        return services.BuildServiceProvider();
    }
}
