using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Yalfan.Application.History;
using Yalfan.Domain.Catalog;

namespace Yalfan.Infrastructure.Persistence.Configurations;

internal sealed class ProductSizeConfiguration : IEntityTypeConfiguration<ProductSize>
{
    public void Configure(EntityTypeBuilder<ProductSize> builder)
    {
        builder.ToTable(AuditedTables.ProductSizes);
        builder.HasKey(size => size.Id);
        builder.Property(size => size.Id).ValueGeneratedNever();

        // Not unique since 1.6.2 (ADR 0023): sizes are dragged into a new order, which rewrites
        // their positions in one save, and PostgreSQL checks a unique index row by row, so a swap
        // would fail half way (the stages' reason, ADR 0022). The model breaks ties by id.
        builder.HasIndex("ProductId", nameof(ProductSize.Position));

        // Size names are unique within one model only, which the Domain checks; the same name
        // ("بزرگ") is normal across models, so there is no unique index.
        builder.OwnsNames(size => size.Names, AuditedTables.ProductSizeTranslations, ProductSize.NameMaxLength, uniqueNames: false);
    }
}
