using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Yalfan.Application.Common;
using Yalfan.Application.Common.Handlers;
using Yalfan.Application.Common.Validation;
using Yalfan.Domain.Common;
using Yalfan.Domain.Parties;

namespace Yalfan.Application.Parties.UpdateContractor;

/// <summary>
/// Saves the profile form. It has no IBAN on purpose: the form never receives the whole IBAN,
/// so it could not send it back, and saving a name must never clear or change an IBAN (ADR 0019).
/// </summary>
public sealed record UpdateContractorCommand(
    Guid Id,
    string? FullName,
    string? MobileNumber,
    IReadOnlyList<Guid>? StageIds,
    string? Notes);

public sealed class UpdateContractorValidator : AbstractValidator<UpdateContractorCommand>
{
    public UpdateContractorValidator()
    {
        RuleFor(command => command.FullName).NotEmpty().MaximumLength(Contractor.FullNameMaxLength);
        RuleFor(command => command.MobileNumber).MustBeMobileNumber();
        RuleFor(command => command.StageIds).NotEmpty().WithErrorCode(PartiesErrors.StageRequired.Code);
        RuleFor(command => command.Notes).MaximumLength(Contractor.NotesMaxLength);
    }
}

internal sealed class UpdateContractorHandler(IAppDbContext db) : ICommandHandler<UpdateContractorCommand, ContractorDetails>
{
    public async Task<Result<ContractorDetails>> Handle(UpdateContractorCommand command, CancellationToken cancellationToken)
    {
        var contractor = await db.Contractors.SingleOrDefaultAsync(c => c.Id == command.Id, cancellationToken);
        if (contractor is null)
        {
            return PartiesErrors.ContractorNotFound;
        }

        var mobile = MobileNumber.Create(command.MobileNumber).Value;
        var stageIds = command.StageIds ?? [];

        if (!await PartyStages.ExistAsync(db, stageIds, cancellationToken))
        {
            return PartiesErrors.StageNotFound;
        }

        if (await ContractorViews.MobileTakenAsync(db, mobile, contractor.Id, cancellationToken))
        {
            return PartiesErrors.MobileTaken;
        }

        var updated = contractor.UpdateProfile(command.FullName, mobile, stageIds, command.Notes);
        if (updated.IsFailure)
        {
            return updated.Error;
        }

        await db.SaveChangesAsync(cancellationToken);

        return await ContractorViews.DetailsAsync(db, contractor, cancellationToken);
    }
}
