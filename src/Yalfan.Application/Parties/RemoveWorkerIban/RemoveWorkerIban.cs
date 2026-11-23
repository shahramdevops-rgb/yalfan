using Microsoft.EntityFrameworkCore;
using Yalfan.Application.Common;
using Yalfan.Application.Common.Handlers;
using Yalfan.Domain.Common;
using Yalfan.Domain.Parties;

namespace Yalfan.Application.Parties.RemoveWorkerIban;

/// <summary>Removes a worker's IBAN, for example when the account is closed. Audited like a change.</summary>
public sealed record RemoveWorkerIbanCommand(Guid Id);

internal sealed class RemoveWorkerIbanHandler(IAppDbContext db)
    : ICommandHandler<RemoveWorkerIbanCommand, WorkerDetails>
{
    public async Task<Result<WorkerDetails>> Handle(RemoveWorkerIbanCommand command, CancellationToken cancellationToken)
    {
        var worker = await db.Workers.SingleOrDefaultAsync(w => w.Id == command.Id, cancellationToken);
        if (worker is null)
        {
            return PartiesErrors.WorkerNotFound;
        }

        worker.RemoveIban();
        await db.SaveChangesAsync(cancellationToken);

        return await WorkerViews.DetailsAsync(db, worker, cancellationToken);
    }
}
