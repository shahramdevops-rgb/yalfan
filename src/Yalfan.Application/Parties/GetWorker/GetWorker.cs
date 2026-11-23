using Microsoft.EntityFrameworkCore;
using Yalfan.Application.Common;
using Yalfan.Application.Common.Handlers;
using Yalfan.Domain.Common;
using Yalfan.Domain.Parties;

namespace Yalfan.Application.Parties.GetWorker;

public sealed record GetWorkerQuery(Guid Id);

internal sealed class GetWorkerHandler(IAppDbContext db) : IQueryHandler<GetWorkerQuery, WorkerDetails>
{
    public async Task<Result<WorkerDetails>> Handle(GetWorkerQuery query, CancellationToken cancellationToken)
    {
        var worker = await db.Workers.AsNoTracking().SingleOrDefaultAsync(w => w.Id == query.Id, cancellationToken);
        if (worker is null)
        {
            return PartiesErrors.WorkerNotFound;
        }

        return await WorkerViews.DetailsAsync(db, worker, cancellationToken);
    }
}
