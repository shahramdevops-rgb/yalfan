using Microsoft.EntityFrameworkCore;
using Yalfan.Application.Common;
using Yalfan.Application.Common.Handlers;
using Yalfan.Domain.Common;

namespace Yalfan.Application.Catalog.ListFabrics;

public sealed record ListFabricsQuery;

/// <summary>
/// Every fabric, in the order they were added. The panel sorts by name itself, with Persian
/// alphabetical order, which the database's default collation does not have.
/// </summary>
internal sealed class ListFabricsHandler(IAppDbContext db) : IQueryHandler<ListFabricsQuery, IReadOnlyList<CatalogOptionSummary>>
{
    public async Task<Result<IReadOnlyList<CatalogOptionSummary>>> Handle(ListFabricsQuery query, CancellationToken cancellationToken)
    {
        var fabrics = await db.Fabrics.AsNoTracking().OrderBy(x => x.Id).ToListAsync(cancellationToken);

        return fabrics.Select(x => new CatalogOptionSummary(x.Id, x.PersianName)).ToList();
    }
}
