using Microsoft.EntityFrameworkCore;
using Yalfan.Application.Common;
using Yalfan.Application.Common.Handlers;
using Yalfan.Domain.Common;
using Yalfan.Domain.Parties;

namespace Yalfan.Application.History.GetWorkerHistory;

/// <summary>A worker's change history, with the stages; an IBAN only as set, replaced or removed (ADR 0027).</summary>
public sealed record GetWorkerHistoryQuery(Guid WorkerId, DateTimeOffset? Before);

internal sealed class GetWorkerHistoryHandler(IAppDbContext db) : IQueryHandler<GetWorkerHistoryQuery, HistoryPage>
{
    public async Task<Result<HistoryPage>> Handle(GetWorkerHistoryQuery query, CancellationToken cancellationToken)
    {
        if (!await db.Workers.AnyAsync(worker => worker.Id == query.WorkerId, cancellationToken))
        {
            return PartiesErrors.WorkerNotFound;
        }

        return await PartyHistory.ReadAsync(
            db,
            AuditedTables.Workers,
            AuditedTables.WorkerStages,
            query.WorkerId,
            query.Before,
            cancellationToken);
    }
}
