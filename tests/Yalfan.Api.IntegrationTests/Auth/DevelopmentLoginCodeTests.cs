using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Yalfan.Application.Common.Sms;
using Yalfan.Domain.Identity;
using Yalfan.Infrastructure.Sms;
using static Yalfan.Api.IntegrationTests.Auth.AuthTestHelpers;

namespace Yalfan.Api.IntegrationTests.Auth;

/// <summary>
/// Until the SMS provider is connected, the Development login page shows the login code the log
/// provider kept (ADR 0012). The route must exist in Development only.
/// </summary>
public sealed class DevelopmentLoginCodeTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string Route = "/panel/auth/dev/otp";

    [Fact]
    public async Task Development_shows_the_code_just_sent_and_the_code_logs_in()
    {
        await using var development = InDevelopment();
        using var client = BrowserClient(development);
        var mobile = await factory.CreateUserAsync(Role.Operator);

        Assert.Equal(HttpStatusCode.OK, (await client.RequestCode(mobile)).StatusCode);
        var shown = await client.PostAsJsonAsync(Route, new { mobileNumber = mobile }, Cancel);

        Assert.Equal(HttpStatusCode.OK, shown.StatusCode);
        var code = (await shown.Body()).GetProperty("code").GetString()!;
        Assert.Equal(HttpStatusCode.OK, (await client.Login(mobile, code)).StatusCode);
    }

    [Fact]
    public async Task Development_shows_nothing_for_a_number_that_got_no_code_or_once_the_code_expired()
    {
        await using var development = InDevelopment();
        using var client = BrowserClient(development);
        var mobile = await factory.CreateUserAsync(Role.Operator);

        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync(Route, new { mobileNumber = mobile }, Cancel)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync(Route, new { mobileNumber = "x" }, Cancel)).StatusCode);

        await client.RequestCode(mobile);
        factory.Time.Advance(OtpCode.Lifetime);

        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync(Route, new { mobileNumber = mobile }, Cancel)).StatusCode);
    }

    [Fact]
    public async Task The_route_does_not_exist_outside_development()
    {
        using var anonymous = factory.CreateBrowserClient();
        var mobile = await factory.CreateUserAsync(Role.Operator);
        await anonymous.RequestCode(mobile);

        // An address that does not exist is refused to an anonymous caller like any other, so
        // only a signed-in caller can tell that it is missing.
        var refused = await anonymous.PostAsJsonAsync(Route, new { mobileNumber = mobile }, Cancel);
        using var owner = await factory.CreateClientAsAsync(Role.Owner);
        var missing = await owner.PostAsJsonAsync(Route, new { mobileNumber = mobile }, Cancel);

        Assert.Equal(HttpStatusCode.Unauthorized, refused.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    /// <summary>
    /// The same API in Development, with the real log provider, whose outbox the route reads,
    /// instead of the test's capturing sender. The database and the IBAN key are pinned again,
    /// because Development also reads the developer's user-secrets, which name his own database.
    /// </summary>
    private WebApplicationFactory<Program> InDevelopment() => factory.WithWebHostBuilder(builder =>
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:Database"] = factory.ConnectionString,
                ["Encryption:IbanKey"] = ApiFactory.IbanKey,
            }));
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ISmsSender>();
            services.AddSingleton<ISmsSender>(provider => provider.GetRequiredService<LoggingSmsSender>());
        });
    });

    private static HttpClient BrowserClient(WebApplicationFactory<Program> api) =>
        api.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
}
