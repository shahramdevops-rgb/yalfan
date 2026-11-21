using Microsoft.EntityFrameworkCore;
using Yalfan.Application.Common;
using Yalfan.Application.Common.Handlers;
using Yalfan.Domain.Common;

namespace Yalfan.Application.Catalog.ListColours;

public sealed record ListColoursQuery;

/// <summary>
/// Every colour, in the order they were added. The panel sorts by name itself, with Persian
/// alphabetical order, which the database's default collation does not have.
/// </summary>
internal sealed class ListColoursHandler(IAppDbContext db) : IQueryHandler<ListColoursQuery, IReadOnlyList<CatalogOptionSummary>>
{
    public async Task<Result<IReadOnlyList<CatalogOptionSummary>>> Handle(ListColoursQuery query, CancellationToken cancellationToken)
    {
        var colours = await db.Colours.AsNoTracking().OrderBy(x => x.Id).ToListAsync(cancellationToken);

        return colours.Select(x => new CatalogOptionSummary(x.Id, x.PersianName)).ToList();
    }
}
