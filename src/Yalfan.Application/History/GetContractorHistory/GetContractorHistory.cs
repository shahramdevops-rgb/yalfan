using Microsoft.EntityFrameworkCore;
using Yalfan.Application.Common;
using Yalfan.Application.Common.Handlers;
using Yalfan.Domain.Common;
using Yalfan.Domain.Parties;

namespace Yalfan.Application.History.GetContractorHistory;

/// <summary>A contractor's change history, with his stages; an IBAN only as set, replaced or removed (ADR 0027).</summary>
public sealed record GetContractorHistoryQuery(Guid ContractorId, DateTimeOffset? Before);

internal sealed class GetContractorHistoryHandler(IAppDbContext db) : IQueryHandler<GetContractorHistoryQuery, HistoryPage>
{
    public async Task<Result<HistoryPage>> Handle(GetContractorHistoryQuery query, CancellationToken cancellationToken)
    {
        if (!await db.Contractors.AnyAsync(contractor => contractor.Id == query.ContractorId, cancellationToken))
        {
            return PartiesErrors.ContractorNotFound;
        }

        return await PartyHistory.ReadAsync(
            db,
            AuditedTables.Contractors,
            AuditedTables.ContractorStages,
            query.ContractorId,
            query.Before,
            cancellationToken);
    }
}
