using Microsoft.EntityFrameworkCore;
using Yalfan.Application.Common;
using Yalfan.Domain.Catalog;

namespace Yalfan.Application.Catalog;

/// <summary>
/// A model as the panel shows it, with every name in Persian. The category, fabrics and colours
/// carry their active flag, so the model's page can mark one that has been deactivated since
/// (ADR 0022); sizes have no flag.
/// </summary>
public sealed record ProductDetails(
    Guid Id,
    int Number,
    string Name,
    string? Description,
    CatalogListItem? Category,
    bool IsActive,
    IReadOnlyList<CatalogOptionSummary> Sizes,
    IReadOnlyList<VariantDetails> Variants);

/// <summary>One bag of a model, in the order of its SKU.</summary>
public sealed record VariantDetails(
    Guid Id,
    string Sku,
    CatalogListItem Fabric,
    CatalogListItem Colour,
    CatalogOptionSummary? Size,
    bool IsActive);

/// <summary>
/// Loads a model to change it, and answers it as the panel shows it. Every change on the model's
/// page answers the whole model, so the page shows exactly what was saved, SKUs included.
/// </summary>
internal static class ProductRecords
{
    /// <summary>The model with its sizes and variants, tracked, to be changed and saved.</summary>
    public static Task<Product?> LoadAsync(IAppDbContext db, Guid id, CancellationToken cancellationToken) =>
        db.Products
            .Include(p => p.Sizes)
            .Include(p => p.Variants)
            .SingleOrDefaultAsync(p => p.Id == id, cancellationToken);

    public static async Task<ProductDetails> DetailsAsync(IAppDbContext db, Product product, CancellationToken cancellationToken)
    {
        var category = product.CategoryId is { } categoryId
            ? await db.Categories.AsNoTracking().SingleAsync(c => c.Id == categoryId, cancellationToken)
            : null;

        var fabricIds = product.Variants.Select(v => v.FabricId).Distinct().ToList();
        var fabrics = await db.Fabrics.AsNoTracking()
            .Where(f => fabricIds.Contains(f.Id))
            .ToDictionaryAsync(f => f.Id, f => new CatalogListItem(f.Id, f.PersianName, f.IsActive), cancellationToken);

        var colourIds = product.Variants.Select(v => v.ColourId).Distinct().ToList();
        var colours = await db.Colours.AsNoTracking()
            .Where(c => colourIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => new CatalogListItem(c.Id, c.PersianName, c.IsActive), cancellationToken);

        var sizes = product.Sizes.Select(s => new CatalogOptionSummary(s.Id, s.PersianName)).ToList();

        return new ProductDetails(
            product.Id,
            product.Number,
            product.PersianName,
            product.PersianDescription,
            category is null ? null : new CatalogListItem(category.Id, category.PersianName, category.IsActive),
            product.IsActive,
            sizes,
            product.Variants
                .Select(v => new VariantDetails(
                    v.Id,
                    v.Sku,
                    fabrics[v.FabricId],
                    colours[v.ColourId],
                    v.SizeId is { } sizeId ? sizes.Single(s => s.Id == sizeId) : null,
                    v.IsActive))
                .ToList());
    }
}
