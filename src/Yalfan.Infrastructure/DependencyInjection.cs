using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Yalfan.Application.Catalog;
using Yalfan.Application.Common;
using Yalfan.Application.Common.Security;
using Yalfan.Application.Common.Sms;
using Yalfan.Infrastructure.Catalog;
using Yalfan.Infrastructure.Identity;
using Yalfan.Infrastructure.Persistence;
using Yalfan.Infrastructure.Persistence.Encryption;
using Yalfan.Infrastructure.Persistence.Interceptors;
using Yalfan.Infrastructure.Sms;
using Yalfan.Infrastructure.Time;

namespace Yalfan.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Name of the health check that asks the database whether it can be reached.</summary>
    public const string DatabaseHealthCheckName = "database";

    /// <summary>
    /// Registers the Infrastructure layer: the implementations of the Application interfaces
    /// (persistence, identity, SMS, payments, jobs).
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddPersistence(configuration);
        services.AddIdentity(configuration);
        services.AddSms(configuration);

        // Tests replace TimeProvider to set the time; everything else reads IClock.
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IClock, SystemClock>();

        return services;
    }

    private static void AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        // The connection string is read when the first DbContext is created, not here, so that
        // tests can replace the configuration after the services are registered.
        services.AddDbContext<AppDbContext>((provider, options) => options
            .UseNpgsql(configuration.GetConnectionString(DatabaseOptions.ConnectionStringName))
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(provider.GetRequiredService<AuditInterceptor>()));
        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());
        services.AddScoped<IProductNumbers, ProductNumberSequence>();

        // One audit interceptor per DbContext: it holds the entries of the save in progress.
        services.AddScoped<AuditInterceptor>();

        // Outside an HTTP request (start-up, jobs) nobody is logged in: the audit says "system".
        // The Api registers the real, token-based ICurrentUser after this.
        services.TryAddScoped<ICurrentUser, NoCurrentUser>();

        services.AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SectionName));

        // IBANs are stored encrypted (ADR 0019); without a usable key the API refuses to start.
        services.AddOptions<EncryptionOptions>()
            .Bind(configuration.GetSection(EncryptionOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<EncryptionOptions>, EncryptionOptionsValidator>();

        // Bound here without the validator, so design-time tools (dotnet ef) can create the
        // DbContext without the key; the running API is stopped by the validator above instead.
        services.AddSingleton(_ => new IbanCipher(
            configuration.GetSection(EncryptionOptions.SectionName).Get<EncryptionOptions>() ?? new EncryptionOptions()));

        // Hosted services start in registration order: the schema first, then the first owner.
        services.AddHostedService<DatabaseMigrator>();
        services.AddHostedService<OwnerSeeder>();

        services.AddHealthChecks()
            .AddDbContextCheck<AppDbContext>(DatabaseHealthCheckName);
    }

    private static void AddIdentity(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(
                jwt => Encoding.UTF8.GetByteCount(jwt.SigningKey) >= JwtOptions.MinimumKeyBytes,
                $"Jwt:SigningKey must be at least {JwtOptions.MinimumKeyBytes} bytes. Set it with user-secrets or Jwt__SigningKey.")
            .ValidateOnStart();

        services.AddOptions<SeedOwnerOptions>()
            .Bind(configuration.GetSection(SeedOwnerOptions.SectionName));

        services.AddSingleton<IAccessTokenIssuer, JwtAccessTokenIssuer>();
    }

    private static void AddSms(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<SmsOptions>()
            .Bind(configuration.GetSection(SmsOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<SmsOptions>, SmsOptionsValidator>();

        // The only provider until the real adapter is added before go-live; the validator
        // allows it only in Development and Testing.
        services.AddSingleton<ISmsSender, LoggingSmsSender>();
    }
}
