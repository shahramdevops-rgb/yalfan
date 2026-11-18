using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Yalfan.Domain.Audit;

namespace Yalfan.Infrastructure.Persistence.Configurations;

/// <summary>
/// No foreign key to users: the audit must never fail to save, and a later actor (contractor,
/// customer) is still a user, so the id is enough. A trigger in the migration refuses UPDATE,
/// DELETE and TRUNCATE on this table.
/// </summary>
internal sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable("audit_entries");
        builder.HasKey(entry => entry.Id);
        builder.Property(entry => entry.Id).ValueGeneratedNever();

        builder.Property(entry => entry.Actor).HasConversion<string>().HasMaxLength(10);
        builder.Property(entry => entry.Action).HasConversion<string>().HasMaxLength(10);

        // W3C trace id: 32 hex characters.
        builder.Property(entry => entry.TraceId).HasMaxLength(32);
        builder.Property(entry => entry.EntityType).HasMaxLength(100).IsRequired();
        builder.Property(entry => entry.EntityId).HasMaxLength(100).IsRequired();

        // jsonb, so the history of one field can be queried ("when did this IBAN change").
        builder.Property(entry => entry.Changes).HasColumnType("jsonb").IsRequired();

        // "History of this row" and "what did this user change".
        builder.HasIndex(entry => new { entry.EntityType, entry.EntityId, entry.OccurredAt });
        builder.HasIndex(entry => new { entry.UserId, entry.OccurredAt });
    }
}
