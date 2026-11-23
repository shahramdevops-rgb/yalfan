using Microsoft.EntityFrameworkCore;
using Yalfan.Application.Common;
using Yalfan.Domain.Audit;
using Yalfan.Domain.Catalog;
using Yalfan.Domain.Identity;
using Yalfan.Domain.Parties;
using Yalfan.Domain.Production;
using Yalfan.Infrastructure.Catalog;
using Yalfan.Infrastructure.Persistence.Encryption;

namespace Yalfan.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options, IbanCipher ibanCipher) : DbContext(options), IAppDbContext
{
    public DbSet<User> Users => Set<User>();

    public DbSet<OtpCode> OtpCodes => Set<OtpCode>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Fabric> Fabrics => Set<Fabric>();

    public DbSet<Colour> Colours => Set<Colour>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Stage> Stages => Set<Stage>();

    public DbSet<Contractor> Contractors => Set<Contractor>();

    public DbSet<Worker> Workers => Set<Worker>();

    /// <summary>Written only by the audit interceptor; read by a history screen later (1.6).</summary>
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Each entity is mapped by its own IEntityTypeConfiguration in Persistence/Configurations.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Model numbers (ADR 0016). A sequence, not MAX + 1, so two saves at the same moment
        // never get the same number.
        modelBuilder.HasSequence<int>(ProductNumberSequence.Name).StartsAt(Product.FirstNumber);

        // After the configurations, so it sees every IBAN property they named (ADR 0019).
        modelBuilder.EncryptIbans(ibanCipher);
    }
}
