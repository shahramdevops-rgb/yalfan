using Microsoft.EntityFrameworkCore;
using Yalfan.Application.Common;
using Yalfan.Application.Common.Handlers;
using Yalfan.Domain.Common;
using Yalfan.Domain.Parties;

namespace Yalfan.Application.Parties.RemoveContractorIban;

/// <summary>Removes a contractor's IBAN, for example when the account is closed. Audited like a change.</summary>
public sealed record RemoveContractorIbanCommand(Guid Id);

internal sealed class RemoveContractorIbanHandler(IAppDbContext db)
    : ICommandHandler<RemoveContractorIbanCommand, ContractorDetails>
{
    public async Task<Result<ContractorDetails>> Handle(RemoveContractorIbanCommand command, CancellationToken cancellationToken)
    {
        var contractor = await db.Contractors.SingleOrDefaultAsync(c => c.Id == command.Id, cancellationToken);
        if (contractor is null)
        {
            return PartiesErrors.ContractorNotFound;
        }

        contractor.RemoveIban();
        await db.SaveChangesAsync(cancellationToken);

        return await ContractorViews.DetailsAsync(db, contractor, cancellationToken);
    }
}
