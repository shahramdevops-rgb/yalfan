using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Yalfan.Domain.Common;
using Yalfan.Domain.Parties;
using Yalfan.Domain.Production;

namespace Yalfan.Infrastructure.Persistence.Configurations;

internal sealed class WorkerConfiguration : IEntityTypeConfiguration<Worker>
{
    public void Configure(EntityTypeBuilder<Worker> builder)
    {
        builder.ToTable("workers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        // Persian alphabetical order, as for contractors (ADR 0019).
        builder.Property(x => x.FullName)
            .HasMaxLength(Worker.FullNameMaxLength)
            .UseCollation(PersianCollation.Name)
            .IsRequired();

        builder.Property(x => x.MobileNumber)
            .HasConversion(mobile => mobile.Value, value => MobileNumber.Create(value).Value)
            .HasMaxLength(11)
            .IsRequired();

        // One worker per mobile, also in the database: it will be the worker's login (Phase 10).
        // Not unique across contractors and staff users: one person may be in both (ADR 0020).
        builder.HasIndex(x => x.MobileNumber).IsUnique();

        // A calendar day with no time: PostgreSQL date (ADR 0005).
        builder.Property(x => x.StartDate);

        builder.Property(x => x.Notes).HasMaxLength(Worker.NotesMaxLength);

        // Encrypted and audited without its values, like every IBAN (IbanMapping, ADR 0019).
        builder.Property(x => x.Iban);

        builder.Ignore(x => x.StageIds);

        builder.OwnsMany(x => x.Stages, stage =>
        {
            stage.ToTable("worker_stages");
            stage.WithOwner().HasForeignKey("WorkerId");
            stage.HasKey("WorkerId", nameof(WorkerStage.StageId));

            // Restrict: a stage that workers do must never disappear under them.
            stage.HasOne<Stage>().WithMany().HasForeignKey(x => x.StageId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
