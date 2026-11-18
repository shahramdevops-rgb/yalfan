using Microsoft.EntityFrameworkCore;
using Yalfan.Application.Common;
using Yalfan.Domain.Audit;
using Yalfan.Domain.Identity;

namespace Yalfan.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    public DbSet<User> Users => Set<User>();

    public DbSet<OtpCode> OtpCodes => Set<OtpCode>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    /// <summary>Written only by the audit interceptor; read by a history screen later (1.6).</summary>
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Each entity is mapped by its own IEntityTypeConfiguration in Persistence/Configurations.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
