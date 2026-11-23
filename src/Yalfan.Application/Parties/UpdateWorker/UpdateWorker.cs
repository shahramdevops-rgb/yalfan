using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Yalfan.Application.Common;
using Yalfan.Application.Common.Handlers;
using Yalfan.Application.Common.Validation;
using Yalfan.Domain.Common;
using Yalfan.Domain.Parties;

namespace Yalfan.Application.Parties.UpdateWorker;

/// <summary>
/// Saves the profile form. It has no IBAN on purpose: the form never receives the whole IBAN,
/// so it could not send it back, and saving a name must never clear or change an IBAN (ADR 0019).
/// </summary>
public sealed record UpdateWorkerCommand(
    Guid Id,
    string? FullName,
    string? MobileNumber,
    IReadOnlyList<Guid>? StageIds,
    DateOnly? StartDate,
    string? Notes);

public sealed class UpdateWorkerValidator : AbstractValidator<UpdateWorkerCommand>
{
    public UpdateWorkerValidator(IClock clock)
    {
        RuleFor(command => command.FullName).NotEmpty().MaximumLength(Worker.FullNameMaxLength);
        RuleFor(command => command.MobileNumber).MustBeMobileNumber();
        RuleFor(command => command.StartDate).MustBeStartDate(clock);
        RuleFor(command => command.Notes).MaximumLength(Worker.NotesMaxLength);
    }
}

internal sealed class UpdateWorkerHandler(IAppDbContext db, IClock clock) : ICommandHandler<UpdateWorkerCommand, WorkerDetails>
{
    public async Task<Result<WorkerDetails>> Handle(UpdateWorkerCommand command, CancellationToken cancellationToken)
    {
        var worker = await db.Workers.SingleOrDefaultAsync(w => w.Id == command.Id, cancellationToken);
        if (worker is null)
        {
            return PartiesErrors.WorkerNotFound;
        }

        var mobile = MobileNumber.Create(command.MobileNumber).Value;
        var stageIds = command.StageIds ?? [];

        if (!await PartyStages.ExistAsync(db, stageIds, cancellationToken))
        {
            return PartiesErrors.StageNotFound;
        }

        if (await WorkerViews.MobileTakenAsync(db, mobile, worker.Id, cancellationToken))
        {
            return PartiesErrors.MobileTaken;
        }

        var updated = worker.UpdateProfile(command.FullName, mobile, stageIds, command.StartDate!.Value, command.Notes, clock.Today);
        if (updated.IsFailure)
        {
            return updated.Error;
        }

        await db.SaveChangesAsync(cancellationToken);

        return await WorkerViews.DetailsAsync(db, worker, cancellationToken);
    }
}
