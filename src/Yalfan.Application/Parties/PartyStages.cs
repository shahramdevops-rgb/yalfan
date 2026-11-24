using Microsoft.EntityFrameworkCore;
using Yalfan.Application.Common;
using Yalfan.Application.Production;
using Yalfan.Domain.Common;
using Yalfan.Domain.Parties;

namespace Yalfan.Application.Parties;

/// <summary>
/// The stages ticked on a contractor or a worker. Both keep only the stage ids, and the stages
/// belong to the Production module, so their names, order and state are looked up here.
/// </summary>
internal static class PartyStages
{
    /// <summary>
    /// These stages in the order of the work as the operator dragged it (ADR 0022), so
    /// «برش، دوخت، بسته‌بندی» read in that order. One query for any number of people.
    /// </summary>
    public static Task<List<StageSummary>> InOrderAsync(
        IAppDbContext db,
        IEnumerable<Guid> stageIds,
        CancellationToken cancellationToken)
    {
        var ids = stageIds.Distinct().ToList();
        return db.Stages.AsNoTracking()
            .Where(stage => ids.Contains(stage.Id))
            .OrderBy(stage => stage.Position)
            .ThenBy(stage => stage.Id)
            .Select(stage => new StageSummary(stage.Id, stage.Name, stage.IsActive))
            .ToListAsync(cancellationToken);
    }

    /// <summary>One person's stages, taken from <paramref name="inOrder"/> so they keep its order.</summary>
    public static List<StageSummary> Of(IEnumerable<Guid> stageIds, IEnumerable<StageSummary> inOrder)
    {
        var ticked = stageIds.ToHashSet();
        return inOrder.Where(stage => ticked.Contains(stage.Id)).ToList();
    }

    /// <summary>
    /// Checks the ticked stages, which the Domain cannot do: each must exist, and each that was
    /// not ticked before (<paramref name="alreadyTicked"/>) must be active. A stage deactivated
    /// after it was ticked stays allowed, so saving the profile again never fails on it (ADR 0022).
    /// </summary>
    public static async Task<Result> CheckAsync(
        IAppDbContext db,
        IReadOnlyCollection<Guid> stageIds,
        IReadOnlyCollection<Guid> alreadyTicked,
        CancellationToken cancellationToken)
    {
        var ids = stageIds.Distinct().ToList();
        var found = await db.Stages.AsNoTracking()
            .Where(stage => ids.Contains(stage.Id))
            .Select(stage => new { stage.Id, stage.IsActive })
            .ToListAsync(cancellationToken);

        if (found.Count != ids.Count)
        {
            return PartiesErrors.StageNotFound;
        }

        if (found.Any(stage => !stage.IsActive && !alreadyTicked.Contains(stage.Id)))
        {
            return PartiesErrors.StageInactive;
        }

        return Result.Success();
    }
}
