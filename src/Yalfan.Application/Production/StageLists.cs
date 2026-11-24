using Microsoft.EntityFrameworkCore;
using Yalfan.Application.Common;

namespace Yalfan.Application.Production;

/// <summary>The stages in the order of the work, in one place, so no list is ever ordered another way.</summary>
internal static class StageLists
{
    /// <summary>
    /// Every stage by its position (ADR 0022). The id breaks a tie, which only two stages added
    /// at the same moment can make.
    /// </summary>
    public static Task<List<StageSummary>> InOrderAsync(IAppDbContext db, CancellationToken cancellationToken) =>
        db.Stages.AsNoTracking()
            .OrderBy(stage => stage.Position)
            .ThenBy(stage => stage.Id)
            .Select(stage => new StageSummary(stage.Id, stage.Name, stage.IsActive))
            .ToListAsync(cancellationToken);
}
