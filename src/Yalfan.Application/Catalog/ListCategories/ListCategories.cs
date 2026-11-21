using Microsoft.EntityFrameworkCore;
using Yalfan.Application.Common;
using Yalfan.Application.Common.Handlers;
using Yalfan.Domain.Common;

namespace Yalfan.Application.Catalog.ListCategories;

public sealed record ListCategoriesQuery;

/// <summary>
/// Every category, in the order they were added. The panel sorts by name itself, with Persian
/// alphabetical order, which the database's default collation does not have.
/// </summary>
internal sealed class ListCategoriesHandler(IAppDbContext db) : IQueryHandler<ListCategoriesQuery, IReadOnlyList<CatalogOptionSummary>>
{
    public async Task<Result<IReadOnlyList<CatalogOptionSummary>>> Handle(ListCategoriesQuery query, CancellationToken cancellationToken)
    {
        var categories = await db.Categories.AsNoTracking().OrderBy(x => x.Id).ToListAsync(cancellationToken);

        return categories.Select(x => new CatalogOptionSummary(x.Id, x.PersianName)).ToList();
    }
}
