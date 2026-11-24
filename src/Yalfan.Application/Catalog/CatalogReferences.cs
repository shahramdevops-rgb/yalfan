using Microsoft.EntityFrameworkCore;
using Yalfan.Application.Common;
using Yalfan.Domain.Catalog;
using Yalfan.Domain.Common;

namespace Yalfan.Application.Catalog;

/// <summary>
/// Checks the category, fabrics and colours a request names: each must exist and be active,
/// because a deactivated one is not used for new work (ADR 0022). Two queries for any number of
/// variants. A product keeps only their ids, so the check cannot be left to the Domain.
/// </summary>
internal static class CatalogReferences
{
    public static async Task<Result> CheckAsync(
        IAppDbContext db,
        IReadOnlyCollection<Guid> fabricIds,
        IReadOnlyCollection<Guid> colourIds,
        CancellationToken cancellationToken)
    {
        var distinctFabrics = fabricIds.Distinct().ToList();
        var fabrics = await db.Fabrics.AsNoTracking()
            .Where(f => distinctFabrics.Contains(f.Id))
            .Select(f => f.IsActive)
            .ToListAsync(cancellationToken);
        if (fabrics.Count != distinctFabrics.Count)
        {
            return CatalogErrors.FabricNotFound;
        }

        if (fabrics.Contains(false))
        {
            return CatalogErrors.FabricInactive;
        }

        var distinctColours = colourIds.Distinct().ToList();
        var colours = await db.Colours.AsNoTracking()
            .Where(c => distinctColours.Contains(c.Id))
            .Select(c => c.IsActive)
            .ToListAsync(cancellationToken);
        if (colours.Count != distinctColours.Count)
        {
            return CatalogErrors.ColourNotFound;
        }

        if (colours.Contains(false))
        {
            return CatalogErrors.ColourInactive;
        }

        return Result.Success();
    }

    /// <summary>The model's category, when it has one: it must exist and be active.</summary>
    public static async Task<Result> CheckCategoryAsync(IAppDbContext db, Guid? categoryId, CancellationToken cancellationToken)
    {
        if (categoryId is not { } id)
        {
            return Result.Success();
        }

        var isActive = await db.Categories.AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => (bool?)c.IsActive)
            .SingleOrDefaultAsync(cancellationToken);

        return isActive switch
        {
            null => CatalogErrors.CategoryNotFound,
            false => CatalogErrors.CategoryInactive,
            true => Result.Success(),
        };
    }

    /// <summary>Whether another model has this Persian name; <paramref name="exceptId"/> is the model being renamed.</summary>
    public static Task<bool> ProductNameTakenAsync(
        IAppDbContext db,
        string persianName,
        Guid? exceptId,
        CancellationToken cancellationToken) =>
        db.Products.AnyAsync(
            p => p.Id != exceptId && p.Translations.Any(t => t.Locale == Locale.Fa && t.Name == persianName),
            cancellationToken);
}
