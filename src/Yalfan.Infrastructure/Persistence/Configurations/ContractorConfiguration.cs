using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Yalfan.Application.History;
using Yalfan.Domain.Common;
using Yalfan.Domain.Parties;
using Yalfan.Domain.Production;

namespace Yalfan.Infrastructure.Persistence.Configurations;

internal sealed class ContractorConfiguration : IEntityTypeConfiguration<Contractor>
{
    public void Configure(EntityTypeBuilder<Contractor> builder)
    {
        builder.ToTable(AuditedTables.Contractors);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        // Sorted in Persian alphabetical order wherever it is sorted, whatever locale the server's
        // database was created with (ADR 0019).
        builder.Property(x => x.FullName)
            .HasMaxLength(Contractor.FullNameMaxLength)
            .UseCollation(PersianCollation.Name)
            .IsRequired();

        builder.Property(x => x.MobileNumber)
            .HasConversion(mobile => mobile.Value, value => MobileNumber.Create(value).Value)
            .HasMaxLength(11)
            .IsRequired();

        // One contractor per mobile, also in the database: it will be his login (Phase 10).
        builder.HasIndex(x => x.MobileNumber).IsUnique();

        builder.Property(x => x.Notes).HasMaxLength(Contractor.NotesMaxLength);

        // Encrypted and audited without its values, like every IBAN (IbanMapping, ADR 0019).
        builder.Property(x => x.Iban);

        builder.Ignore(x => x.StageIds);

        builder.OwnsMany(x => x.Stages, stage =>
        {
            stage.ToTable(AuditedTables.ContractorStages);
            stage.WithOwner().HasForeignKey("ContractorId");
            stage.HasKey("ContractorId", nameof(ContractorStage.StageId));

            // Restrict: a stage that contractors do must never disappear under them.
            stage.HasOne<Stage>().WithMany().HasForeignKey(x => x.StageId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
