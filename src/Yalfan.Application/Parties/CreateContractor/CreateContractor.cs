using FluentValidation;
using Yalfan.Application.Common;
using Yalfan.Application.Common.Handlers;
using Yalfan.Application.Common.Validation;
using Yalfan.Domain.Common;
using Yalfan.Domain.Parties;

namespace Yalfan.Application.Parties.CreateContractor;

/// <summary>
/// A new contractor from the operator's one form. The IBAN is optional; an empty one means none,
/// because an empty form field arrives as "".
/// </summary>
public sealed record CreateContractorCommand(
    string? FullName,
    string? MobileNumber,
    IReadOnlyList<Guid>? StageIds,
    string? Notes,
    string? Iban);

public sealed class CreateContractorValidator : AbstractValidator<CreateContractorCommand>
{
    public CreateContractorValidator()
    {
        RuleFor(command => command.FullName).NotEmpty().MaximumLength(Contractor.FullNameMaxLength);
        RuleFor(command => command.MobileNumber).MustBeMobileNumber();
        RuleFor(command => command.StageIds).NotEmpty().WithErrorCode(PartiesErrors.StageRequired.Code);
        RuleFor(command => command.Notes).MaximumLength(Contractor.NotesMaxLength);
        RuleFor(command => command.Iban).MustBeIban().When(command => !string.IsNullOrWhiteSpace(command.Iban));
    }
}

internal sealed class CreateContractorHandler(IAppDbContext db, IClock clock)
    : ICommandHandler<CreateContractorCommand, ContractorDetails>
{
    public async Task<Result<ContractorDetails>> Handle(CreateContractorCommand command, CancellationToken cancellationToken)
    {
        var mobile = MobileNumber.Create(command.MobileNumber).Value;
        var stageIds = command.StageIds ?? [];

        var created = Contractor.Create(command.FullName, mobile, stageIds, command.Notes, clock.UtcNow);
        if (created.IsFailure)
        {
            return created.Error;
        }

        var contractor = created.Value;

        if (!string.IsNullOrWhiteSpace(command.Iban))
        {
            contractor.ChangeIban(Iban.Create(command.Iban).Value);
        }

        if (!await ContractorViews.StagesExistAsync(db, stageIds, cancellationToken))
        {
            return PartiesErrors.StageNotFound;
        }

        // Checked here for a coded answer; the unique index stops a duplicate saved at the same moment.
        if (await ContractorViews.MobileTakenAsync(db, mobile, null, cancellationToken))
        {
            return PartiesErrors.MobileTaken;
        }

        db.Contractors.Add(contractor);
        await db.SaveChangesAsync(cancellationToken);

        return await ContractorViews.DetailsAsync(db, contractor, cancellationToken);
    }
}
