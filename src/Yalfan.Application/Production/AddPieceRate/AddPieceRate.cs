using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Yalfan.Application.Common;
using Yalfan.Application.Common.Handlers;
using Yalfan.Domain.Common;
using Yalfan.Domain.Production;

namespace Yalfan.Application.Production.AddPieceRate;

/// <summary>
/// A new entry in the rate card: the pay for one piece of a model at a stage, in Rial, from a
/// start date on (ADR 0021). The start date may be in the past or the future. Entering a rate
/// with the start date of an existing entry corrects that entry; the old one stays, replaced.
/// </summary>
public sealed record AddPieceRateCommand(Guid? ProductId, Guid? StageId, long? Amount, DateOnly? StartsOn);

public sealed class AddPieceRateValidator : AbstractValidator<AddPieceRateCommand>
{
    public AddPieceRateValidator()
    {
        RuleFor(command => command.ProductId).NotEmpty();
        RuleFor(command => command.StageId).NotEmpty();
        RuleFor(command => command.Amount).NotNull().GreaterThan(0);
        RuleFor(command => command.StartsOn).NotNull();
    }
}

internal sealed class AddPieceRateHandler(IAppDbContext db, IClock clock)
    : ICommandHandler<AddPieceRateCommand, PieceRateEntry>
{
    public async Task<Result<PieceRateEntry>> Handle(AddPieceRateCommand command, CancellationToken cancellationToken)
    {
        var productId = command.ProductId!.Value;
        var stageId = command.StageId!.Value;

        var productIsActive = await db.Products.AsNoTracking()
            .Where(product => product.Id == productId)
            .Select(product => (bool?)product.IsActive)
            .SingleOrDefaultAsync(cancellationToken);
        if (productIsActive is null)
        {
            return ProductionErrors.ProductNotFound;
        }

        // Nothing new is given out for a deactivated model; its old rates stay (ADR 0023).
        if (productIsActive is false)
        {
            return ProductionErrors.ProductInactive;
        }

        var stage = await db.Stages.AsNoTracking().SingleOrDefaultAsync(s => s.Id == stageId, cancellationToken);
        if (stage is null)
        {
            return ProductionErrors.StageNotFound;
        }

        if (!stage.IsActive)
        {
            return ProductionErrors.StageInactive;
        }

        var created = PieceRate.Create(
            productId,
            stageId,
            Money.Rial(command.Amount!.Value),
            command.StartsOn!.Value,
            clock.UtcNow);
        if (created.IsFailure)
        {
            return created.Error;
        }

        var rate = created.Value;
        db.PieceRates.Add(rate);
        await db.SaveChangesAsync(cancellationToken);

        return new PieceRateEntry(
            rate.Id,
            StageSummary.Of(stage),
            rate.Amount.Amount,
            rate.StartsOn,
            rate.EnteredAt,
            IsReplaced: false);
    }
}
