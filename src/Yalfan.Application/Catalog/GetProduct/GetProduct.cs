using Microsoft.EntityFrameworkCore;
using Yalfan.Application.Common;
using Yalfan.Application.Common.Handlers;
using Yalfan.Domain.Catalog;
using Yalfan.Domain.Common;

namespace Yalfan.Application.Catalog.GetProduct;

public sealed record GetProductQuery(Guid Id);

/// <summary>A model as the panel shows it, with every name in Persian.</summary>
public sealed record ProductDetails(
    Guid Id,
    int Number,
    string Name,
    string? Description,
    CatalogOptionSummary? Category,
    bool IsActive,
    IReadOnlyList<CatalogOptionSummary> Sizes,
    IReadOnlyList<VariantDetails> Variants);

public sealed record VariantDetails(
    Guid Id,
    string Sku,
    CatalogOptionSummary Fabric,
    CatalogOptionSummary Colour,
    CatalogOptionSummary? Size,
    bool IsActive);

internal sealed class GetProductHandler(IAppDbContext db) : IQueryHandler<GetProductQuery, ProductDetails>
{
    public async Task<Result<ProductDetails>> Handle(GetProductQuery query, CancellationToken cancellationToken)
    {
        var product = await db.Products
            .AsNoTracking()
            .Include(p => p.Sizes)
            .Include(p => p.Variants)
            .SingleOrDefaultAsync(p => p.Id == query.Id, cancellationToken);
        if (product is null)
        {
            return CatalogErrors.ProductNotFound;
        }

        var category = product.CategoryId is { } categoryId
            ? await db.Categories.AsNoTracking().SingleAsync(c => c.Id == categoryId, cancellationToken)
            : null;

        var fabricIds = product.Variants.Select(v => v.FabricId).Distinct().ToList();
        var fabrics = await db.Fabrics.AsNoTracking()
            .Where(f => fabricIds.Contains(f.Id))
            .ToDictionaryAsync(f => f.Id, f => f.PersianName, cancellationToken);

        var colourIds = product.Variants.Select(v => v.ColourId).Distinct().ToList();
        var colours = await db.Colours.AsNoTracking()
            .Where(c => colourIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.PersianName, cancellationToken);

        var sizes = product.Sizes.Select(s => new CatalogOptionSummary(s.Id, s.PersianName)).ToList();

        return new ProductDetails(
            product.Id,
            product.Number,
            product.PersianName,
            product.PersianDescription,
            category is null ? null : new CatalogOptionSummary(category.Id, category.PersianName),
            product.IsActive,
            sizes,
            product.Variants
                .Select(v => new VariantDetails(
                    v.Id,
                    v.Sku,
                    new CatalogOptionSummary(v.FabricId, fabrics[v.FabricId]),
                    new CatalogOptionSummary(v.ColourId, colours[v.ColourId]),
                    v.SizeId is { } sizeId ? sizes.Single(s => s.Id == sizeId) : null,
                    v.IsActive))
                .ToList());
    }
}
