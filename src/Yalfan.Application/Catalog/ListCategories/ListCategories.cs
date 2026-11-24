using Microsoft.EntityFrameworkCore;
using Yalfan.Application.Common;
using Yalfan.Application.Common.Handlers;
using Yalfan.Domain.Common;

namespace Yalfan.Application.Catalog.ListCategories;

public sealed record ListCategoriesQuery;

/// <summary>
/// Every category, active or not, in the order they were added. The panel sorts by name itself,
/// with Persian alphabetical order, which the database's default collation does not have, and
/// its pickers leave the inactive ones out (ADR 0022).
/// </summary>
internal sealed class ListCategoriesHandler(IAppDbContext db) : IQueryHandler<ListCategoriesQuery, IReadOnlyList<CatalogListItem>>
{
    public async Task<Result<IReadOnlyList<CatalogListItem>>> Handle(ListCategoriesQuery query, CancellationToken cancellationToken)
    {
        var categories = await db.Categories.AsNoTracking().OrderBy(x => x.Id).ToListAsync(cancellationToken);

        return categories.Select(x => new CatalogListItem(x.Id, x.PersianName, x.IsActive)).ToList();
    }
}
