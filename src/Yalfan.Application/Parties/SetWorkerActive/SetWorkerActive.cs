using Microsoft.EntityFrameworkCore;
using Yalfan.Application.Common;
using Yalfan.Application.Common.Handlers;
using Yalfan.Domain.Common;
using Yalfan.Domain.Parties;

namespace Yalfan.Application.Parties.SetWorkerActive;

/// <summary>
/// Deactivates a worker who has left, or activates them again. There is no delete and, for now,
/// no leaving date: attendance and payslips will point at the worker, and the rules of a final
/// settlement are not known yet (decided 1405/09/02, ADR 0020).
/// </summary>
public sealed record SetWorkerActiveCommand(Guid Id, bool IsActive);

internal sealed class SetWorkerActiveHandler(IAppDbContext db)
    : ICommandHandler<SetWorkerActiveCommand, WorkerDetails>
{
    public async Task<Result<WorkerDetails>> Handle(SetWorkerActiveCommand command, CancellationToken cancellationToken)
    {
        var worker = await db.Workers.SingleOrDefaultAsync(w => w.Id == command.Id, cancellationToken);
        if (worker is null)
        {
            return PartiesErrors.WorkerNotFound;
        }

        if (command.IsActive)
        {
            worker.Activate();
        }
        else
        {
            worker.Deactivate();
        }

        await db.SaveChangesAsync(cancellationToken);

        return await WorkerViews.DetailsAsync(db, worker, cancellationToken);
    }
}
