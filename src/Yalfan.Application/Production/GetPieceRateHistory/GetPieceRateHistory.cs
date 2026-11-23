using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Yalfan.Application.Common;
using Yalfan.Application.Common.Handlers;
using Yalfan.Domain.Common;
using Yalfan.Domain.Production;

namespace Yalfan.Application.Production.GetPieceRateHistory;

/// <summary>
/// Every rate ever entered for one model, or for one stage of it when
/// <paramref name="StageId"/> is given: past, current, future and replaced (ADR 0021).
/// </summary>
public sealed record GetPieceRateHistoryQuery(Guid? ProductId, Guid? StageId);

public sealed class GetPieceRateHistoryValidator : AbstractValidator<GetPieceRateHistoryQuery>
{
    public GetPieceRateHistoryValidator()
    {
        RuleFor(query => query.ProductId).NotEmpty();
    }
}

/// <summary>
/// Grouped by stage in the order of the work, and within a stage the newest first: the latest
/// start date, then the latest entry. No paging: a model has a few entries per stage a year.
/// </summary>
internal sealed class GetPieceRateHistoryHandler(IAppDbContext db)
    : IQueryHandler<GetPieceRateHistoryQuery, IReadOnlyList<PieceRateEntry>>
{
    public async Task<Result<IReadOnlyList<PieceRateEntry>>> Handle(
        GetPieceRateHistoryQuery query,
        CancellationToken cancellationToken)
    {
        var productId = query.ProductId!.Value;

        if (!await db.Products.AnyAsync(product => product.Id == productId, cancellationToken))
        {
            return ProductionErrors.ProductNotFound;
        }

        var stages = await PieceRateLookups.StagesAsync(db, cancellationToken);
        if (query.StageId is { } stageId && stages.All(stage => stage.Id != stageId))
        {
            return ProductionErrors.StageNotFound;
        }

        var entries = await db.PieceRates.AsNoTracking()
            .Where(rate => rate.ProductId == productId)
            .Where(rate => query.StageId == null || rate.StageId == query.StageId)
            .ToListAsync(cancellationToken);

        var history = stages
            .SelectMany(stage => RateCard.NewestFirst(entries.Where(entry => entry.StageId == stage.Id))
                .Select(entry => new PieceRateEntry(
                    entry.Id,
                    stage,
                    entry.Amount.Amount,
                    entry.StartsOn,
                    entry.EnteredAt,
                    RateCard.IsReplaced(entry, entries))))
            .ToList();

        return history;
    }
}
