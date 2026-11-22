using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Yalfan.Application.Common;
using Yalfan.Application.Common.Handlers;
using Yalfan.Domain.Common;
using Yalfan.Domain.Production;

namespace Yalfan.Application.Production.RenameStage;

public sealed record RenameStageCommand(Guid Id, string? Name);

public sealed class RenameStageValidator : AbstractValidator<RenameStageCommand>
{
    public RenameStageValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(Stage.NameMaxLength);
    }
}

internal sealed class RenameStageHandler(IAppDbContext db) : ICommandHandler<RenameStageCommand, StageSummary>
{
    public async Task<Result<StageSummary>> Handle(RenameStageCommand command, CancellationToken cancellationToken)
    {
        var stage = await db.Stages.SingleOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (stage is null)
        {
            return ProductionErrors.StageNotFound;
        }

        var renamed = stage.Rename(command.Name);
        if (renamed.IsFailure)
        {
            return renamed.Error;
        }

        if (await db.Stages.AnyAsync(x => x.Id != command.Id && x.Name == stage.Name, cancellationToken))
        {
            return ProductionErrors.NameTaken;
        }

        await db.SaveChangesAsync(cancellationToken);

        return new StageSummary(stage.Id, stage.Name);
    }
}
