using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Yalfan.Domain.Catalog;

namespace Yalfan.Infrastructure.Persistence.Configurations;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");
        builder.HasKey(product => product.Id);
        builder.Property(product => product.Id).ValueGeneratedNever();

        // The number people read; taken from the sequence by the handler (ADR 0016).
        builder.HasIndex(product => product.Number).IsUnique();

        // Restrict everywhere in the catalog: nothing here is deleted, and a delete must never
        // cascade into the models that work orders and stock will point at.
        builder.HasOne<Category>().WithMany().HasForeignKey(product => product.CategoryId).OnDelete(DeleteBehavior.Restrict);

        builder.OwnsMany(product => product.Translations, translation =>
        {
            translation.ToTable("product_translations");
            translation.WithOwner().HasForeignKey("ProductId");
            translation.Property(t => t.Locale)
                .HasConversion(TranslationMapping.LocaleConverter)
                .HasMaxLength(TranslationMapping.LocaleMaxLength);
            translation.HasKey("ProductId", nameof(ProductTranslation.Locale));
            translation.Property(t => t.Name).HasMaxLength(Product.NameMaxLength).IsRequired();
            translation.Property(t => t.Description).HasMaxLength(Product.DescriptionMaxLength);

            // No two models share a name (decided 1405/09/01).
            translation.HasIndex(t => new { t.Locale, t.Name }).IsUnique();
        });

        builder.HasMany(product => product.Sizes).WithOne().HasForeignKey("ProductId").OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(product => product.Variants).WithOne().HasForeignKey("ProductId").OnDelete(DeleteBehavior.Restrict);

        // The getters return sorted copies; EF reads and fills the lists behind them.
        builder.Navigation(product => product.Sizes).HasField("_sizes").UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(product => product.Variants).HasField("_variants").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
