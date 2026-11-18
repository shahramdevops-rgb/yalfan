using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using Npgsql;
using Yalfan.Api.Common.Endpoints;
using Yalfan.Api.Endpoints.Panel;
using Yalfan.Api.IntegrationTests.Infrastructure;
using Yalfan.Api.IntegrationTests.Sample;
using Yalfan.Application.Common.Handlers;
using Yalfan.Application.Common.Sms;
using Yalfan.Domain.Common;
using Yalfan.Domain.Identity;
using Yalfan.Infrastructure.Persistence;

namespace Yalfan.Api.IntegrationTests;

/// <summary>
/// The real API in memory, on its own PostgreSQL database, with a clock the test moves and an
/// SMS sender the test reads. The "Testing" environment keeps Development settings out.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string SeededOwnerMobile = "09120000000";

    private static int _nextMobile = 1000;
    private string _connectionString = string.Empty;

    public FakeTimeProvider Time { get; } = new(TimeProvider.System.GetUtcNow());

    public CapturingSmsSender Sms { get; } = new();

    public async ValueTask InitializeAsync()
    {
        var container = await PostgresServer.GetAsync();
        _connectionString = new NpgsqlConnectionStringBuilder(container.GetConnectionString())
        {
            Database = $"yalfan_test_{Guid.NewGuid():N}",
        }.ConnectionString;
    }

    /// <summary>A mobile number no other test uses, so codes and limits never mix.</summary>
    public static string NextMobile() =>
        "0912" + Interlocked.Increment(ref _nextMobile).ToString("D7", CultureInfo.InvariantCulture);

    /// <summary>Adds a user straight to the database; returns his mobile.</summary>
    public async Task<string> CreateUserAsync(Role role)
    {
        var mobile = NextMobile();
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Users.Add(User.Create($"Test {role}", MobileNumber.Create(mobile).Value, role, Time.GetUtcNow()).Value);
        await db.SaveChangesAsync();
        return mobile;
    }

    public async Task DeactivateAsync(string mobile)
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.ExecuteSqlAsync($"update users set is_active = false where mobile_number = {mobile}");
    }

    /// <summary>A client on https, so the Secure refresh cookie is kept and sent like in a browser.</summary>
    public HttpClient CreateBrowserClient() =>
        CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });

    /// <summary>Asks for a code, reads it from the captured SMS and logs in.</summary>
    public async Task<SessionResponse> LoginAsync(HttpClient client, string mobile)
    {
        var cancel = TestContext.Current.CancellationToken;
        (await client.PostAsJsonAsync("/panel/auth/otp", new { mobileNumber = mobile }, cancel)).EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync(
            "/panel/auth/login", new { mobileNumber = mobile, code = Sms.LastCodeFor(mobile) }, cancel);
        response.EnsureSuccessStatusCode();

        var session = (await response.Content.ReadFromJsonAsync<SessionResponse>(Json.Options, cancel))!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        return session;
    }

    /// <summary>A logged-in client for a new user with this role.</summary>
    public async Task<HttpClient> CreateClientAsAsync(Role role)
    {
        var client = CreateBrowserClient();
        await LoginAsync(client, await CreateUserAsync(role));
        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Serilog:MinimumLevel:Default", "Warning");
        builder.UseSetting("ConnectionStrings:Database", _connectionString);
        builder.UseSetting("Database:MigrateOnStartup", "true");
        builder.UseSetting("Sms:Provider", "Log");
        builder.UseSetting("Jwt:SigningKey", Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)));
        builder.UseSetting("Identity:SeedOwner:FullName", "Seeded Owner");
        builder.UseSetting("Identity:SeedOwner:MobileNumber", SeededOwnerMobile);

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Time);
            services.RemoveAll<ISmsSender>();
            services.AddSingleton<ISmsSender>(Sms);

            services.AddUseCases(typeof(ApiFactory).Assembly);
            services.AddSingleton<IEndpointModule, SampleEndpoints>();
        });
    }
}
