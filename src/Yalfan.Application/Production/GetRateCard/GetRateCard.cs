using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Yalfan.Application.Common;
using Yalfan.Application.Common.Handlers;
using Yalfan.Domain.Common;
using Yalfan.Domain.Production;

namespace Yalfan.Application.Production.GetRateCard;

/// <summary>
/// The rates of one model in force on a day; <paramref name="On"/> empty means the workshop's
/// today (Tehran). This is the rate a work order issued that day copies (task 2.2).
/// </summary>
public sealed record GetRateCardQuery(Guid? ProductId, DateOnly? On);

/// <summary>
/// Every stage in the order of the work, each with the rate in force on <paramref name="On"/>.
/// A stage with no rate yet is listed without one, so the screen shows what is still missing.
/// </summary>
public sealed record RateCardDetails(
    Guid ProductId,
    int ProductNumber,
    string ProductName,
    DateOnly On,
    IReadOnlyList<StageRate> Stages);

/// <summary>The rate of one stage: the pay for one piece in Rial and the day it started; both null when there is none.</summary>
public sealed record StageRate(StageSummary Stage, long? Amount, DateOnly? StartsOn);

public sealed class GetRateCardValidator : AbstractValidator<GetRateCardQuery>
{
    public GetRateCardValidator()
    {
        RuleFor(query => query.ProductId).NotEmpty();
    }
}

internal sealed class GetRateCardHandler(IAppDbContext db, IClock clock)
    : IQueryHandler<GetRateCardQuery, RateCardDetails>
{
    public async Task<Result<RateCardDetails>> Handle(GetRateCardQuery query, CancellationToken cancellationToken)
    {
        var product = await PieceRateLookups.ProductAsync(db, query.ProductId!.Value, cancellationToken);
        if (product is null)
        {
            return ProductionErrors.ProductNotFound;
        }

        var on = query.On ?? clock.Today;
        var stages = await PieceRateLookups.StagesAsync(db, cancellationToken);

        // Every entry of the model that has started by that day: a few per stage, so the rule of
        // which one is in force runs in the Domain, not in SQL.
        var entries = await db.PieceRates.AsNoTracking()
            .Where(rate => rate.ProductId == product.Id && rate.StartsOn <= on)
            .ToListAsync(cancellationToken);

        var rates = stages
            .Select(stage =>
            {
                var inForce = RateCard.InForceOn(entries, product.Id, stage.Id, on);
                return new StageRate(stage, inForce?.Amount.Amount, inForce?.StartsOn);
            })
            .ToList();

        return new RateCardDetails(product.Id, product.Number, product.PersianName, on, rates);
    }
}
