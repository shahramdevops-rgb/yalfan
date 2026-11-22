using Microsoft.EntityFrameworkCore;
using Yalfan.Application.Common;
using Yalfan.Application.Common.Handlers;
using Yalfan.Domain.Common;
using Yalfan.Domain.Parties;

namespace Yalfan.Application.Parties.GetContractor;

public sealed record GetContractorQuery(Guid Id);

internal sealed class GetContractorHandler(IAppDbContext db) : IQueryHandler<GetContractorQuery, ContractorDetails>
{
    public async Task<Result<ContractorDetails>> Handle(GetContractorQuery query, CancellationToken cancellationToken)
    {
        var contractor = await db.Contractors.AsNoTracking().SingleOrDefaultAsync(c => c.Id == query.Id, cancellationToken);
        if (contractor is null)
        {
            return PartiesErrors.ContractorNotFound;
        }

        return await ContractorViews.DetailsAsync(db, contractor, cancellationToken);
    }
}
