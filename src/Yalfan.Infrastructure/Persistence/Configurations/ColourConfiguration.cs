using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Yalfan.Domain.Catalog;

namespace Yalfan.Infrastructure.Persistence.Configurations;

internal sealed class ColourConfiguration : IEntityTypeConfiguration<Colour>
{
    public void Configure(EntityTypeBuilder<Colour> builder)
    {
        builder.ToTable("colours");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        // Unique names, also in the database: the handler's check cannot see a duplicate saved
        // at the same moment.
        builder.OwnsNames(x => x.Names, "colour_translations", Colour.NameMaxLength, uniqueNames: true);
    }
}
