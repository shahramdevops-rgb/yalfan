using Yalfan.Domain.Common;
using Yalfan.Domain.Production;

namespace Yalfan.Domain.Tests.Production;

public class PieceRateTests
{
    private static readonly Guid Product = Guid.CreateVersion7();
    private static readonly Guid Stage = Guid.CreateVersion7();
    private static readonly DateOnly Day = new(2026, 11, 23);
    private static readonly DateTimeOffset Now = new(2026, 11, 23, 9, 30, 0, TimeSpan.Zero);

    [Fact]
    public void A_rate_keeps_what_was_entered()
    {
        var rate = PieceRate.Create(Product, Stage, Money.Rial(1_200_000), Day, Now).Value;

        Assert.Equal(Product, rate.ProductId);
        Assert.Equal(Stage, rate.StageId);
        Assert.Equal(Money.Rial(1_200_000), rate.Amount);
        Assert.Equal(Day, rate.StartsOn);
        Assert.Equal(Now, rate.EnteredAt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void A_rate_is_more_than_zero(long amount)
    {
        Assert.Equal(ProductionErrors.RateNotPositive, PieceRate.Create(Product, Stage, Money.Rial(amount), Day, Now).Error);
    }

    [Fact]
    public void A_rate_is_paid_in_rial()
    {
        Assert.Throws<ArgumentException>(() => PieceRate.Create(Product, Stage, new Money(500, Currency.Usd), Day, Now));
    }

    [Fact]
    public void A_rate_names_a_model_and_a_stage()
    {
        Assert.Throws<ArgumentException>(() => PieceRate.Create(Guid.Empty, Stage, Money.Rial(1), Day, Now));
        Assert.Throws<ArgumentException>(() => PieceRate.Create(Product, Guid.Empty, Money.Rial(1), Day, Now));
    }
}
