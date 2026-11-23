namespace Yalfan.Domain.Production;

/// <summary>
/// Which <see cref="PieceRate"/> is in force, read from the append-only entries (ADR 0021).
/// Nothing about it is stored: an entry that has been replaced keeps its row, and the answer is
/// worked out again from the entries every time.
/// </summary>
public static class RateCard
{
    /// <summary>
    /// The entry in force on <paramref name="day"/> for one stage of one model, or null when no
    /// rate has started by then: of the entries that have started, the one with the latest start
    /// date; of several with that start date, the one entered last.
    /// </summary>
    public static PieceRate? InForceOn(IEnumerable<PieceRate> entries, Guid productId, Guid stageId, DateOnly day) =>
        NewestFirst(entries.Where(entry =>
                entry.ProductId == productId && entry.StageId == stageId && entry.StartsOn <= day))
            .FirstOrDefault();

    /// <summary>
    /// True when a later entry for the same model, stage and start date replaced this one: the
    /// correction of a mistyped rate. An entry followed by one with a later start date is not
    /// replaced; it was simply in force until then.
    /// </summary>
    public static bool IsReplaced(PieceRate entry, IEnumerable<PieceRate> entries)
    {
        ArgumentNullException.ThrowIfNull(entry);

        return entries.Any(other =>
            other.ProductId == entry.ProductId
            && other.StageId == entry.StageId
            && other.StartsOn == entry.StartsOn
            && EnteredLater(other, entry));
    }

    /// <summary>
    /// Latest start date first, then the latest entry first. The id only breaks a tie between two
    /// entries typed at the same instant, so the order is always the same.
    /// </summary>
    public static IOrderedEnumerable<PieceRate> NewestFirst(IEnumerable<PieceRate> entries) =>
        entries
            .OrderByDescending(entry => entry.StartsOn)
            .ThenByDescending(entry => entry.EnteredAt)
            .ThenByDescending(entry => entry.Id);

    private static bool EnteredLater(PieceRate other, PieceRate entry) =>
        other.EnteredAt > entry.EnteredAt
        || (other.EnteredAt == entry.EnteredAt && other.Id.CompareTo(entry.Id) > 0);
}
