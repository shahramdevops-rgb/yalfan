using Microsoft.EntityFrameworkCore;
using Yalfan.Application.Common;
using Yalfan.Domain.Catalog;

namespace Yalfan.Application.Production;

/// <summary>
/// One entry of the rate card as the panel shows it. <paramref name="Amount"/> is the pay for one
/// piece in Rial (ADR 0002). <paramref name="IsReplaced"/> is true when a later entry with the
/// same start date corrected it (ADR 0021).
/// </summary>
public sealed record PieceRateEntry(
    Guid Id,
    StageSummary Stage,
    long Amount,
    DateOnly StartsOn,
    DateTimeOffset EnteredAt,
    bool IsReplaced);

internal static class PieceRateLookups
{
    /// <summary>The model a rate names, with its translations for the Persian name; null when it does not exist.</summary>
    public static Task<Product?> ProductAsync(IAppDbContext db, Guid productId, CancellationToken cancellationToken) =>
        db.Products.AsNoTracking().SingleOrDefaultAsync(p => p.Id == productId, cancellationToken);

    /// <summary>
    /// Every stage in the order of the stage list (by id, ADR 0018), so a rate card reads
    /// «برش، دوخت، بسته‌بندی» in the order of the work.
    /// </summary>
    public static Task<List<StageSummary>> StagesAsync(IAppDbContext db, CancellationToken cancellationToken) =>
        db.Stages.AsNoTracking()
            .OrderBy(stage => stage.Id)
            .Select(stage => new StageSummary(stage.Id, stage.Name))
            .ToListAsync(cancellationToken);
}
