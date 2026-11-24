using Microsoft.EntityFrameworkCore;
using Yalfan.Application.Common;
using Yalfan.Application.Common.Handlers;
using Yalfan.Domain.Common;

namespace Yalfan.Application.Production.ListStages;

public sealed record ListStagesQuery;

/// <summary>
/// Every stage, active or not, in the order of the work as the operator dragged it (ADR 0022).
/// The panel shows the list as it comes and never sorts it by name; its pickers leave the
/// inactive ones out.
/// </summary>
internal sealed class ListStagesHandler(IAppDbContext db) : IQueryHandler<ListStagesQuery, IReadOnlyList<StageSummary>>
{
    public async Task<Result<IReadOnlyList<StageSummary>>> Handle(ListStagesQuery query, CancellationToken cancellationToken) =>
        await StageLists.InOrderAsync(db, cancellationToken);
}
