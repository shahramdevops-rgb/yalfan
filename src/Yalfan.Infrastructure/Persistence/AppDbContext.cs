using Microsoft.EntityFrameworkCore;
using Yalfan.Application.Common;
using Yalfan.Domain.Audit;
using Yalfan.Domain.Catalog;
using Yalfan.Domain.Identity;
using Yalfan.Domain.Production;
using Yalfan.Infrastructure.Catalog;

namespace Yalfan.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    public DbSet<User> Users => Set<User>();

    public DbSet<OtpCode> OtpCodes => Set<OtpCode>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Fabric> Fabrics => Set<Fabric>();

    public DbSet<Colour> Colours => Set<Colour>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Stage> Stages => Set<Stage>();

    /// <summary>Written only by the audit interceptor; read by a history screen later (1.6).</summary>
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Each entity is mapped by its own IEntityTypeConfiguration in Persistence/Configurations.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Model numbers (ADR 0016). A sequence, not MAX + 1, so two saves at the same moment
        // never get the same number.
        modelBuilder.HasSequence<int>(ProductNumberSequence.Name).StartsAt(Product.FirstNumber);
    }
}
