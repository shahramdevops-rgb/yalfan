using Microsoft.EntityFrameworkCore;
using Yalfan.Application.Common;
using Yalfan.Application.Production;
using Yalfan.Domain.Parties;

namespace Yalfan.Application.Parties;

/// <summary>
/// A contractor as the panel shows him. The IBAN is only ever masked here (ADR 0019); the whole
/// number is a separate, owner-only request.
/// </summary>
public sealed record ContractorDetails(
    Guid Id,
    string FullName,
    string MobileNumber,
    string? Notes,
    IReadOnlyList<StageSummary> Stages,
    string? MaskedIban,
    bool IsActive,
    DateTimeOffset CreatedAt);

internal static class ContractorViews
{
    public static async Task<ContractorDetails> DetailsAsync(
        IAppDbContext db,
        Contractor contractor,
        CancellationToken cancellationToken)
    {
        var names = await PartyStages.NamesAsync(db, contractor.StageIds, cancellationToken);

        return new ContractorDetails(
            contractor.Id,
            contractor.FullName,
            contractor.MobileNumber.Value,
            contractor.Notes,
            PartyStages.Of(contractor.StageIds, names),
            contractor.Iban?.Masked,
            contractor.IsActive,
            contractor.CreatedAt);
    }

    public static Task<bool> MobileTakenAsync(
        IAppDbContext db,
        Domain.Common.MobileNumber mobile,
        Guid? exceptContractorId,
        CancellationToken cancellationToken) =>
        db.Contractors.AnyAsync(c => c.MobileNumber == mobile && c.Id != exceptContractorId, cancellationToken);
}
