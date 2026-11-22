using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Yalfan.Domain.Production;

namespace Yalfan.Infrastructure.Persistence.Configurations;

/// <summary>
/// The shared stage list (ADR 0018). The workshop's three stages are inserted once by the
/// migration that creates the table, not here, so later renames by the operator are never
/// compared against seed data.
/// </summary>
internal sealed class StageConfiguration : IEntityTypeConfiguration<Stage>
{
    public void Configure(EntityTypeBuilder<Stage> builder)
    {
        builder.ToTable("stages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Name).HasMaxLength(Stage.NameMaxLength).IsRequired();

        // Unique names, also in the database: the handler's check cannot see a duplicate saved
        // at the same moment.
        builder.HasIndex(x => x.Name).IsUnique();
    }
}
