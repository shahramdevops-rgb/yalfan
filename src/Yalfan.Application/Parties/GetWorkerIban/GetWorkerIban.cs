using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Yalfan.Application.Common;
using Yalfan.Application.Common.Handlers;
using Yalfan.Application.Common.Security;
using Yalfan.Domain.Common;
using Yalfan.Domain.Parties;

namespace Yalfan.Application.Parties.GetWorkerIban;

/// <summary>
/// The whole IBAN, for the owner only (the endpoint allows nobody else), as for contractors
/// (ADR 0019, ADR 0020). Each time it is shown, a log line records who saw whose IBAN, because
/// the audit records changes, not reads.
/// </summary>
public sealed record GetWorkerIbanQuery(Guid Id);

/// <summary>The whole number, <c>IR</c> and 24 digits.</summary>
public sealed record WorkerIbanResponse(string Iban);

internal sealed partial class GetWorkerIbanHandler(
    IAppDbContext db,
    ICurrentUser currentUser,
    ILogger<GetWorkerIbanHandler> logger)
    : IQueryHandler<GetWorkerIbanQuery, WorkerIbanResponse>
{
    public async Task<Result<WorkerIbanResponse>> Handle(GetWorkerIbanQuery query, CancellationToken cancellationToken)
    {
        var worker = await db.Workers.AsNoTracking().SingleOrDefaultAsync(w => w.Id == query.Id, cancellationToken);
        if (worker is null)
        {
            return PartiesErrors.WorkerNotFound;
        }

        if (worker.Iban is null)
        {
            return PartiesErrors.IbanNotSet;
        }

        LogIbanShown(logger, currentUser.UserId, worker.Id);

        return new WorkerIbanResponse(worker.Iban.Value);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "The whole IBAN of worker {WorkerId} was shown to user {UserId}")]
    private static partial void LogIbanShown(ILogger logger, Guid? userId, Guid workerId);
}
