using Microsoft.EntityFrameworkCore;
using Yalfan.Application.Common;
using Yalfan.Application.Production;

namespace Yalfan.Application.Parties;

/// <summary>
/// The stages ticked on a contractor or a worker. Both keep only the stage ids, and the stages
/// belong to the Production module, so their names and existence are looked up here.
/// </summary>
internal static class PartyStages
{
    /// <summary>The Persian names of these stages, in one query.</summary>
    public static Task<Dictionary<Guid, string>> NamesAsync(
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
    public static List<StageSummary> Of(IEnumerable<Guid> stageIds, Dictionary<Guid, string> names) =>
        stageIds.Order().Select(id => new StageSummary(id, names[id])).ToList();

    /// <summary>Checks that every ticked stage exists, which the Domain cannot do.</summary>
    public static async Task<bool> ExistAsync(
        IAppDbContext db,
        IReadOnlyCollection<Guid> stageIds,
        CancellationToken cancellationToken)
    {
        var ids = stageIds.Distinct().ToList();
        return await db.Stages.CountAsync(stage => ids.Contains(stage.Id), cancellationToken) == ids.Count;
    }
}
