using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Yalfan.Application.Common;
using Yalfan.Application.Common.Handlers;
using Yalfan.Domain.Common;
using Yalfan.Domain.Production;

namespace Yalfan.Application.Production.CreateStage;

public sealed record CreateStageCommand(string? Name);

public sealed class CreateStageValidator : AbstractValidator<CreateStageCommand>
{
    public CreateStageValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(Stage.NameMaxLength);
    }
}

internal sealed class CreateStageHandler(IAppDbContext db) : ICommandHandler<CreateStageCommand, StageSummary>
{
    public async Task<Result<StageSummary>> Handle(CreateStageCommand command, CancellationToken cancellationToken)
    {
        var created = Stage.Create(command.Name);
        if (created.IsFailure)
        {
            return created.Error;
        }

        var stage = created.Value;

        // Checked here for a coded answer; the unique index stops a duplicate that slips past at
        // the same moment.
        if (await db.Stages.AnyAsync(x => x.Name == stage.Name, cancellationToken))
        {
            return ProductionErrors.NameTaken;
        }

        db.Stages.Add(stage);
        await db.SaveChangesAsync(cancellationToken);

        return new StageSummary(stage.Id, stage.Name);
    }
}
