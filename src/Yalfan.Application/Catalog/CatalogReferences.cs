using Microsoft.EntityFrameworkCore;
using Yalfan.Application.Common;
using Yalfan.Domain.Catalog;
using Yalfan.Domain.Common;

namespace Yalfan.Application.Catalog;

/// <summary>
/// Checks that the fabrics and colours a request names exist, in two queries for any number of
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
        var foundFabrics = await db.Fabrics.CountAsync(f => distinctFabrics.Contains(f.Id), cancellationToken);
        if (foundFabrics != distinctFabrics.Count)
        {
            return CatalogErrors.FabricNotFound;
        }

        var distinctColours = colourIds.Distinct().ToList();
        var foundColours = await db.Colours.CountAsync(c => distinctColours.Contains(c.Id), cancellationToken);
        if (foundColours != distinctColours.Count)
        {
            return CatalogErrors.ColourNotFound;
        }

        return Result.Success();
    }

    public static Task<bool> ProductNameTakenAsync(IAppDbContext db, string persianName, CancellationToken cancellationToken) =>
        db.Products.AnyAsync(
            p => p.Translations.Any(t => t.Locale == Locale.Fa && t.Name == persianName),
            cancellationToken);
}
