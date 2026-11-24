using FluentValidation;
using Yalfan.Application.Common;
using Yalfan.Application.Common.Handlers;
using Yalfan.Application.Common.Validation;
using Yalfan.Domain.Common;
using Yalfan.Domain.Parties;

namespace Yalfan.Application.Parties.CreateWorker;

/// <summary>
/// A new worker from the operator's one form. The stages may be empty (a worker with no
/// production stage); the IBAN is optional, and an empty one means none, because an empty form
/// field arrives as "".
/// </summary>
public sealed record CreateWorkerCommand(
    string? FullName,
    string? MobileNumber,
    IReadOnlyList<Guid>? StageIds,
    DateOnly? StartDate,
    string? Notes,
    string? Iban);

public sealed class CreateWorkerValidator : AbstractValidator<CreateWorkerCommand>
{
    public CreateWorkerValidator(IClock clock)
    {
        RuleFor(command => command.FullName).NotEmpty().MaximumLength(Worker.FullNameMaxLength);
        RuleFor(command => command.MobileNumber).MustBeMobileNumber();
        RuleFor(command => command.StartDate).MustBeStartDate(clock);
        RuleFor(command => command.Notes).MaximumLength(Worker.NotesMaxLength);
        RuleFor(command => command.Iban).MustBeIban().When(command => !string.IsNullOrWhiteSpace(command.Iban));
    }
}

internal sealed class CreateWorkerHandler(IAppDbContext db, IClock clock)
    : ICommandHandler<CreateWorkerCommand, WorkerDetails>
{
    public async Task<Result<WorkerDetails>> Handle(CreateWorkerCommand command, CancellationToken cancellationToken)
    {
        var mobile = MobileNumber.Create(command.MobileNumber).Value;
        var stageIds = command.StageIds ?? [];

        var created = Worker.Create(
            command.FullName,
            mobile,
            stageIds,
            command.StartDate!.Value,
            command.Notes,
            clock.Today,
            clock.UtcNow);
        if (created.IsFailure)
        {
            return created.Error;
        }

        var worker = created.Value;

        if (!string.IsNullOrWhiteSpace(command.Iban))
        {
            worker.ChangeIban(Iban.Create(command.Iban).Value);
        }

        var stages = await PartyStages.CheckAsync(db, stageIds, [], cancellationToken);
        if (stages.IsFailure)
        {
            return stages.Error;
        }

        // Checked here for a coded answer; the unique index stops a duplicate saved at the same moment.
        if (await WorkerViews.MobileTakenAsync(db, mobile, null, cancellationToken))
        {
            return PartiesErrors.MobileTaken;
        }

        db.Workers.Add(worker);
        await db.SaveChangesAsync(cancellationToken);

        return await WorkerViews.DetailsAsync(db, worker, cancellationToken);
    }
}
