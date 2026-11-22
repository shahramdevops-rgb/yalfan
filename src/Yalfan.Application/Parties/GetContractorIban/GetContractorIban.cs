using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Yalfan.Application.Common;
using Yalfan.Application.Common.Handlers;
using Yalfan.Application.Common.Security;
using Yalfan.Domain.Common;
using Yalfan.Domain.Parties;

namespace Yalfan.Application.Parties.GetContractorIban;

/// <summary>
/// The whole IBAN, for the owner only (the endpoint allows nobody else): until Phase 15 he pays
/// by hand and needs the number, while nobody else needs it once it is typed (decided 1405/09/02,
/// ADR 0019). Each time it is shown, a log line records who saw whose IBAN, because the audit
/// records changes, not reads.
/// </summary>
public sealed record GetContractorIbanQuery(Guid Id);

/// <summary>The whole number, <c>IR</c> and 24 digits.</summary>
public sealed record ContractorIbanResponse(string Iban);

internal sealed partial class GetContractorIbanHandler(
    IAppDbContext db,
    ICurrentUser currentUser,
    ILogger<GetContractorIbanHandler> logger)
    : IQueryHandler<GetContractorIbanQuery, ContractorIbanResponse>
{
    public async Task<Result<ContractorIbanResponse>> Handle(GetContractorIbanQuery query, CancellationToken cancellationToken)
    {
        var contractor = await db.Contractors.AsNoTracking().SingleOrDefaultAsync(c => c.Id == query.Id, cancellationToken);
        if (contractor is null)
        {
            return PartiesErrors.ContractorNotFound;
        }

        if (contractor.Iban is null)
        {
            return PartiesErrors.IbanNotSet;
        }

        LogIbanShown(logger, currentUser.UserId, contractor.Id);

        return new ContractorIbanResponse(contractor.Iban.Value);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "The whole IBAN of contractor {ContractorId} was shown to user {UserId}")]
    private static partial void LogIbanShown(ILogger logger, Guid? userId, Guid contractorId);
}
