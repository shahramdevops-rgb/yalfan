using Microsoft.EntityFrameworkCore;
using Yalfan.Application.Common;
using Yalfan.Application.Common.Handlers;
using Yalfan.Domain.Common;

namespace Yalfan.Application.Production.ListStages;

public sealed record ListStagesQuery;

/// <summary>
/// Every stage, in the order they were added. The first three are added by the database
/// migration in the order of the work (cutting, sewing, packing), so the panel shows the list as
/// it comes and does not sort it by name (ADR 0018).
/// </summary>
internal sealed class ListStagesHandler(IAppDbContext db) : IQueryHandler<ListStagesQuery, IReadOnlyList<StageSummary>>
{
    public async Task<Result<IReadOnlyList<StageSummary>>> Handle(ListStagesQuery query, CancellationToken cancellationToken)
    {
        var stages = await db.Stages.AsNoTracking()
            .OrderBy(x => x.Id)
            .Select(x => new StageSummary(x.Id, x.Name))
            .ToListAsync(cancellationToken);

        return stages;
    }
}
