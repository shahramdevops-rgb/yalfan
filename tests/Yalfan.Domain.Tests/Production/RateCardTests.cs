using Yalfan.Domain.Common;
using Yalfan.Domain.Production;

namespace Yalfan.Domain.Tests.Production;

/// <summary>Which entry of the append-only rate card is in force (ADR 0021).</summary>
public class RateCardTests
{
    private static readonly Guid Product = Guid.CreateVersion7();
    private static readonly Guid Sewing = Guid.CreateVersion7();
    private static readonly Guid Packing = Guid.CreateVersion7();
    private static readonly DateTimeOffset Morning = new(2026, 11, 23, 6, 0, 0, TimeSpan.Zero);

    [Fact]
    public void No_rate_is_in_force_before_the_first_one_starts()
    {
        List<PieceRate> entries = [Rate(Sewing, 100, new DateOnly(2026, 12, 1))];

        Assert.Null(RateCard.InForceOn(entries, Product, Sewing, new DateOnly(2026, 11, 30)));
    }

    [Fact]
    public void A_rate_is_in_force_from_its_start_date_until_the_next_one_starts()
    {
        var first = Rate(Sewing, 100, new DateOnly(2026, 1, 1));
        var raise = Rate(Sewing, 120, new DateOnly(2026, 12, 1));
        List<PieceRate> entries = [raise, first];

        Assert.Equal(first, RateCard.InForceOn(entries, Product, Sewing, new DateOnly(2026, 1, 1)));
        Assert.Equal(first, RateCard.InForceOn(entries, Product, Sewing, new DateOnly(2026, 11, 30)));
        Assert.Equal(raise, RateCard.InForceOn(entries, Product, Sewing, new DateOnly(2026, 12, 1)));
        Assert.Equal(raise, RateCard.InForceOn(entries, Product, Sewing, new DateOnly(2030, 1, 1)));
    }

    [Fact]
    public void A_later_entry_with_the_same_start_date_replaces_the_mistyped_one()
    {
        var mistyped = Rate(Sewing, 12_000, new DateOnly(2026, 11, 1), Morning);
        var corrected = Rate(Sewing, 120_000, new DateOnly(2026, 11, 1), Morning.AddMinutes(2));
        List<PieceRate> entries = [corrected, mistyped];

        Assert.Equal(corrected, RateCard.InForceOn(entries, Product, Sewing, new DateOnly(2026, 11, 23)));
        Assert.True(RateCard.IsReplaced(mistyped, entries));
        Assert.False(RateCard.IsReplaced(corrected, entries));
    }

    [Fact]
    public void A_rate_followed_by_a_later_start_date_is_past_not_replaced()
    {
        var first = Rate(Sewing, 100, new DateOnly(2026, 1, 1), Morning.AddDays(1));
        var raise = Rate(Sewing, 120, new DateOnly(2026, 12, 1), Morning);
        List<PieceRate> entries = [first, raise];

        Assert.False(RateCard.IsReplaced(first, entries));
        Assert.False(RateCard.IsReplaced(raise, entries));
    }

    [Fact]
    public void An_entry_with_an_earlier_start_date_typed_later_does_not_undo_a_later_start_date()
    {
        // "From 1 December 120" is entered first; "from 15 November 110" afterwards. On
        // 1 December the 120 starts all the same: the start dates decide, not the typing order.
        var december = Rate(Sewing, 120, new DateOnly(2026, 12, 1), Morning);
        var november = Rate(Sewing, 110, new DateOnly(2026, 11, 15), Morning.AddHours(1));
        List<PieceRate> entries = [december, november];

        Assert.Equal(november, RateCard.InForceOn(entries, Product, Sewing, new DateOnly(2026, 11, 20)));
        Assert.Equal(december, RateCard.InForceOn(entries, Product, Sewing, new DateOnly(2026, 12, 1)));
    }

    [Fact]
    public void Each_stage_and_model_has_its_own_rates()
    {
        var sewing = Rate(Sewing, 100, new DateOnly(2026, 1, 1));
        var otherModel = PieceRate.Create(Guid.CreateVersion7(), Packing, Money.Rial(70), new DateOnly(2026, 1, 1), Morning).Value;
        List<PieceRate> entries = [sewing, otherModel];

        Assert.Equal(sewing, RateCard.InForceOn(entries, Product, Sewing, new DateOnly(2026, 6, 1)));
        Assert.Null(RateCard.InForceOn(entries, Product, Packing, new DateOnly(2026, 6, 1)));
    }

    [Fact]
    public void Newest_first_orders_by_start_date_then_by_entry()
    {
        var old = Rate(Sewing, 100, new DateOnly(2026, 1, 1), Morning.AddDays(5));
        var mistyped = Rate(Sewing, 12, new DateOnly(2026, 6, 1), Morning);
        var corrected = Rate(Sewing, 120, new DateOnly(2026, 6, 1), Morning.AddHours(1));

        Assert.Equal([corrected, mistyped, old], RateCard.NewestFirst([mistyped, old, corrected]));
    }

    private static PieceRate Rate(Guid stage, long rial, DateOnly startsOn, DateTimeOffset? enteredAt = null) =>
        PieceRate.Create(Product, stage, Money.Rial(rial), startsOn, enteredAt ?? Morning).Value;
}
