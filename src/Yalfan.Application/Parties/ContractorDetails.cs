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
        var names = await StageNamesAsync(db, contractor.StageIds, cancellationToken);

        return new ContractorDetails(
            contractor.Id,
            contractor.FullName,
            contractor.MobileNumber.Value,
            contractor.Notes,
            StagesOf(contractor.StageIds, names),
            contractor.Iban?.Masked,
            contractor.IsActive,
            contractor.CreatedAt);
    }

    /// <summary>The Persian names of these stages, in one query.</summary>
    public static Task<Dictionary<Guid, string>> StageNamesAsync(
        IAppDbContext db,
        IEnumerable<Guid> stageIds,
        CancellationToken cancellationToken)
    {
        var ids = stageIds.Distinct().ToList();
        return db.Stages.AsNoTracking()
            .Where(stage => ids.Contains(stage.Id))
            .ToDictionaryAsync(stage => stage.Id, stage => stage.Name, cancellationToken);
    }

    /// <summary>
    /// In the order of the stage list (by id, ADR 0018), so «برش، دوخت، بسته‌بندی» read in the
    /// order of the work.
    /// </summary>
    public static List<StageSummary> StagesOf(IEnumerable<Guid> stageIds, Dictionary<Guid, string> names) =>
        stageIds.Order().Select(id => new StageSummary(id, names[id])).ToList();

    /// <summary>
    /// Checks that every ticked stage exists. A contractor keeps only their ids, and the stages
    /// belong to the Production module, so the Domain cannot check it.
    /// </summary>
    public static async Task<bool> StagesExistAsync(
        IAppDbContext db,
        IReadOnlyCollection<Guid> stageIds,
        CancellationToken cancellationToken)
    {
        var ids = stageIds.Distinct().ToList();
        return await db.Stages.CountAsync(stage => ids.Contains(stage.Id), cancellationToken) == ids.Count;
    }

    public static Task<bool> MobileTakenAsync(
        IAppDbContext db,
        Domain.Common.MobileNumber mobile,
        Guid? exceptContractorId,
        CancellationToken cancellationToken) =>
        db.Contractors.AnyAsync(c => c.MobileNumber == mobile && c.Id != exceptContractorId, cancellationToken);
}
