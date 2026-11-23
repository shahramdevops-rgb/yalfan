using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Yalfan.Application.Common;
using Yalfan.Application.Common.Handlers;
using Yalfan.Application.Common.Validation;
using Yalfan.Domain.Common;
using Yalfan.Domain.Parties;

namespace Yalfan.Application.Parties.ChangeWorkerIban;

/// <summary>
/// Sets or replaces a worker's IBAN, on its own request like a contractor's (ADR 0019): Phase 15
/// puts the check against the holder's national ID and a holding period here. Every staff role
/// may use it; the audit records the change without the number.
/// </summary>
public sealed record ChangeWorkerIbanCommand(Guid Id, string? Iban);

public sealed class ChangeWorkerIbanValidator : AbstractValidator<ChangeWorkerIbanCommand>
{
    public ChangeWorkerIbanValidator()
    {
        RuleFor(command => command.Iban).MustBeIban();
    }
}

internal sealed class ChangeWorkerIbanHandler(IAppDbContext db)
    : ICommandHandler<ChangeWorkerIbanCommand, WorkerDetails>
{
    public async Task<Result<WorkerDetails>> Handle(ChangeWorkerIbanCommand command, CancellationToken cancellationToken)
    {
        var worker = await db.Workers.SingleOrDefaultAsync(w => w.Id == command.Id, cancellationToken);
        if (worker is null)
        {
            return PartiesErrors.WorkerNotFound;
        }

        worker.ChangeIban(Iban.Create(command.Iban).Value);
        await db.SaveChangesAsync(cancellationToken);

        return await WorkerViews.DetailsAsync(db, worker, cancellationToken);
    }
}
