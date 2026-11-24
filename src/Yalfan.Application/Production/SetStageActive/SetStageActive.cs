using Microsoft.EntityFrameworkCore;
using Yalfan.Application.Common;
using Yalfan.Application.Common.Handlers;
using Yalfan.Domain.Common;
using Yalfan.Domain.Production;

namespace Yalfan.Application.Production.SetStageActive;

/// <summary>
/// Deactivates a stage, or activates it again. A deactivated stage keeps its place in the order
/// of the work, so activating it puts it back where it was (ADR 0022).
/// </summary>
public sealed record SetStageActiveCommand(Guid Id, bool IsActive);

internal sealed class SetStageActiveHandler(IAppDbContext db) : ICommandHandler<SetStageActiveCommand, StageSummary>
{
    public async Task<Result<StageSummary>> Handle(SetStageActiveCommand command, CancellationToken cancellationToken)
    {
        var stage = await db.Stages.SingleOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (stage is null)
        {
            return ProductionErrors.StageNotFound;
        }

        if (command.IsActive)
        {
            stage.Activate();
        }
        else
        {
            stage.Deactivate();
        }

        await db.SaveChangesAsync(cancellationToken);

        return StageSummary.Of(stage);
    }
}
