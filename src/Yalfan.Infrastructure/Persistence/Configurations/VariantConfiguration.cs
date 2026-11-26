using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Yalfan.Application.History;
using Yalfan.Domain.Catalog;

namespace Yalfan.Infrastructure.Persistence.Configurations;

internal sealed class VariantConfiguration : IEntityTypeConfiguration<Variant>
{
    public void Configure(EntityTypeBuilder<Variant> builder)
    {
        builder.ToTable(AuditedTables.ProductVariants);
        builder.HasKey(variant => variant.Id);
        builder.Property(variant => variant.Id).ValueGeneratedNever();

        builder.Property(variant => variant.Sku).HasMaxLength(Variant.SkuMaxLength).IsRequired();
        builder.HasIndex(variant => variant.Sku).IsUnique();
        builder.HasIndex("ProductId", nameof(Variant.Number)).IsUnique();

        // One variant per fabric, colour and size in a model. NULLS NOT DISTINCT, so two
        // variants without a size also count as the same.
        builder.HasIndex("ProductId", nameof(Variant.FabricId), nameof(Variant.ColourId), nameof(Variant.SizeId))
            .IsUnique()
            .AreNullsDistinct(false);

        builder.HasOne<Fabric>().WithMany().HasForeignKey(variant => variant.FabricId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Colour>().WithMany().HasForeignKey(variant => variant.ColourId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ProductSize>().WithMany().HasForeignKey(variant => variant.SizeId).OnDelete(DeleteBehavior.Restrict);
    }
}
