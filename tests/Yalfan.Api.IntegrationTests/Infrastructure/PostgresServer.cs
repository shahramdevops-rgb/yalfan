using Testcontainers.PostgreSql;

namespace Yalfan.Api.IntegrationTests.Infrastructure;

/// <summary>
/// One PostgreSQL container for the whole test run; each test class gets its own database in
/// it. The container is removed when the run ends.
/// </summary>
internal static class PostgresServer
{
    private static readonly Lazy<Task<PostgreSqlContainer>> Container = new(StartAsync);

    public static Task<PostgreSqlContainer> GetAsync() => Container.Value;

    private static async Task<PostgreSqlContainer> StartAsync()
    {
        // The same major version as docker-compose.yml.
        var container = new PostgreSqlBuilder("postgres:18").Build();
        await container.StartAsync();
        return container;
    }
}
