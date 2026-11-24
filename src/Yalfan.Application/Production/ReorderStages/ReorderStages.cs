using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Yalfan.Application.Common;
using Yalfan.Application.Common.Handlers;
using Yalfan.Domain.Common;
using Yalfan.Domain.Production;

namespace Yalfan.Application.Production.ReorderStages;

/// <summary>
/// The whole stage list in its new order, as the operator left it after dragging a stage
/// (ADR 0022). The whole list, not "move one stage", so the request says exactly what the
/// operator saw, and a list made from an old copy is refused instead of half applied.
/// </summary>
public sealed record ReorderStagesCommand(IReadOnlyList<Guid>? StageIds);

public sealed class ReorderStagesValidator : AbstractValidator<ReorderStagesCommand>
{
    /// <summary>Far more stages than a workshop has; stops a runaway request, not the workshop.</summary>
    public const int MaxStages = 200;

    public ReorderStagesValidator()
    {
        RuleFor(command => command.StageIds)
            .NotEmpty()
            .Must(ids => ids!.Count <= MaxStages)
            .Must(ids => ids!.Distinct().Count() == ids!.Count)
            .WithErrorCode(ProductionErrors.StageOrderOutdated.Code);
    }
}

internal sealed class ReorderStagesHandler(IAppDbContext db) : ICommandHandler<ReorderStagesCommand, IReadOnlyList<StageSummary>>
{
    public async Task<Result<IReadOnlyList<StageSummary>>> Handle(ReorderStagesCommand command, CancellationToken cancellationToken)
    {
        var stages = await db.Stages.ToListAsync(cancellationToken);

        var reordered = Stage.Reorder(stages, command.StageIds!);
        if (reordered.IsFailure)
        {
            return reordered.Error;
        }

        // Only the stages whose place really changed are written, and each is audited.
        await db.SaveChangesAsync(cancellationToken);

        return await StageLists.InOrderAsync(db, cancellationToken);
    }
}
