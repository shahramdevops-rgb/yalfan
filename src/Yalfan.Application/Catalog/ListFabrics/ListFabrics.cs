using Microsoft.EntityFrameworkCore;
using Yalfan.Application.Common;
using Yalfan.Application.Common.Handlers;
using Yalfan.Domain.Common;

namespace Yalfan.Application.Catalog.ListFabrics;

public sealed record ListFabricsQuery;

/// <summary>
/// Every fabric, active or not, in the order they were added. The panel sorts by name itself,
/// with Persian alphabetical order, which the database's default collation does not have, and
/// its pickers leave the inactive ones out (ADR 0022).
/// </summary>
internal sealed class ListFabricsHandler(IAppDbContext db) : IQueryHandler<ListFabricsQuery, IReadOnlyList<CatalogListItem>>
{
    public async Task<Result<IReadOnlyList<CatalogListItem>>> Handle(ListFabricsQuery query, CancellationToken cancellationToken)
    {
        var fabrics = await db.Fabrics.AsNoTracking().OrderBy(x => x.Id).ToListAsync(cancellationToken);

        return fabrics.Select(x => new CatalogListItem(x.Id, x.PersianName, x.IsActive)).ToList();
    }
}
