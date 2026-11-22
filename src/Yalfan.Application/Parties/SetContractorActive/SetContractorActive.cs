using Microsoft.EntityFrameworkCore;
using Yalfan.Application.Common;
using Yalfan.Application.Common.Handlers;
using Yalfan.Domain.Common;
using Yalfan.Domain.Parties;

namespace Yalfan.Application.Parties.SetContractorActive;

/// <summary>
/// Deactivates a contractor, or activates him again. There is no delete: work orders and his
/// account (Phase 2) will point at him (decided 1405/09/02).
/// </summary>
public sealed record SetContractorActiveCommand(Guid Id, bool IsActive);

internal sealed class SetContractorActiveHandler(IAppDbContext db)
    : ICommandHandler<SetContractorActiveCommand, ContractorDetails>
{
    public async Task<Result<ContractorDetails>> Handle(SetContractorActiveCommand command, CancellationToken cancellationToken)
    {
        var contractor = await db.Contractors.SingleOrDefaultAsync(c => c.Id == command.Id, cancellationToken);
        if (contractor is null)
        {
            return PartiesErrors.ContractorNotFound;
        }

        if (command.IsActive)
        {
            contractor.Activate();
        }
        else
        {
            contractor.Deactivate();
        }

        await db.SaveChangesAsync(cancellationToken);

        return await ContractorViews.DetailsAsync(db, contractor, cancellationToken);
    }
}
