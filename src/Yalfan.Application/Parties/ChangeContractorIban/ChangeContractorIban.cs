using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Yalfan.Application.Common;
using Yalfan.Application.Common.Handlers;
using Yalfan.Application.Common.Validation;
using Yalfan.Domain.Common;
using Yalfan.Domain.Parties;

namespace Yalfan.Application.Parties.ChangeContractorIban;

/// <summary>
/// Sets or replaces a contractor's IBAN. Its own request, apart from the profile, because it is
/// the field a payout fraud changes: Phase 15 puts verification and a holding period here
/// (ADR 0019). Every staff role may use it (decided 1405/09/02); the audit records the change.
/// </summary>
public sealed record ChangeContractorIbanCommand(Guid Id, string? Iban);

public sealed class ChangeContractorIbanValidator : AbstractValidator<ChangeContractorIbanCommand>
{
    public ChangeContractorIbanValidator()
    {
        RuleFor(command => command.Iban).MustBeIban();
    }
}

internal sealed class ChangeContractorIbanHandler(IAppDbContext db)
    : ICommandHandler<ChangeContractorIbanCommand, ContractorDetails>
{
    public async Task<Result<ContractorDetails>> Handle(ChangeContractorIbanCommand command, CancellationToken cancellationToken)
    {
        var contractor = await db.Contractors.SingleOrDefaultAsync(c => c.Id == command.Id, cancellationToken);
        if (contractor is null)
        {
            return PartiesErrors.ContractorNotFound;
        }

        contractor.ChangeIban(Iban.Create(command.Iban).Value);
        await db.SaveChangesAsync(cancellationToken);

        return await ContractorViews.DetailsAsync(db, contractor, cancellationToken);
    }
}
