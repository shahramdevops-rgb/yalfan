using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Yalfan.Domain.Catalog;

namespace Yalfan.Infrastructure.Persistence.Configurations;

internal sealed class ProductSizeConfiguration : IEntityTypeConfiguration<ProductSize>
{
    public void Configure(EntityTypeBuilder<ProductSize> builder)
    {
        builder.ToTable("product_sizes");
        builder.HasKey(size => size.Id);
        builder.Property(size => size.Id).ValueGeneratedNever();

        builder.HasIndex("ProductId", nameof(ProductSize.Position)).IsUnique();

        // Size names are unique within one model only, which the Domain checks; the same name
        // ("بزرگ") is normal across models, so there is no unique index.
        builder.OwnsNames(size => size.Names, "product_size_translations", ProductSize.NameMaxLength, uniqueNames: false);
    }
}
