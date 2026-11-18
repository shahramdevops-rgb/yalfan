namespace Yalfan.Infrastructure.Persistence;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>Name of the connection string that points at the PostgreSQL database.</summary>
    public const string ConnectionStringName = "Database";

    /// <summary>
    /// Apply pending migrations when the API starts. Off unless configuration turns it on, so
    /// that starting the API can never change a database schema by surprise.
    /// </summary>
    public bool MigrateOnStartup { get; set; }
}
