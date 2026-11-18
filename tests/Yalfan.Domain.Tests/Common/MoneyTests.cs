using System.Globalization;

namespace Yalfan.Domain.Tests.Common;

public class MoneyTests
{
    [Fact]
    public void Rial_creates_an_amount_in_IRR()
    {
        var money = Money.Rial(1_500_000);

        Assert.Equal(1_500_000, money.Amount);
        Assert.Equal(Currency.Irr, money.Currency);
    }

    [Fact]
    public void Two_amounts_with_the_same_number_and_currency_are_equal()
    {
        Assert.Equal(Money.Rial(250_000), Money.Rial(250_000));
        Assert.NotEqual(Money.Rial(250_000), Money.Rial(250_001));
        Assert.NotEqual(Money.Rial(250_000), new Money(250_000, Currency.Usd));
    }

    [Fact]
    public void Add_and_subtract_work_on_whole_units()
    {
        var earned = Money.Rial(9_200_000);
        var advance = Money.Rial(2_000_000);

        Assert.Equal(Money.Rial(11_200_000), earned + advance);
        Assert.Equal(Money.Rial(7_200_000), earned - advance);
    }

    [Fact]
    public void Subtracting_more_than_there_is_gives_a_negative_amount()
    {
        var balance = Money.Rial(1_000_000) - Money.Rial(1_500_000);

        Assert.Equal(Money.Rial(-500_000), balance);
        Assert.True(balance.IsNegative);
    }

    [Fact]
    public void An_amount_plus_its_negation_is_zero()
    {
        var entry = Money.Rial(3_750_000);

        var afterReversal = entry + entry.Negate();

        Assert.True(afterReversal.IsZero);
        Assert.Equal(Money.Zero(Currency.Irr), afterReversal);
    }

    [Fact]
    public void Adding_many_entries_loses_nothing()
    {
        // 0.1 + 0.2 is not 0.3 in floating point. With whole Rials a long sum is exact.
        var total = Money.Zero(Currency.Irr);
        for (var i = 0; i < 100_000; i++)
        {
            total += Money.Rial(333_333);
        }

        Assert.Equal(Money.Rial(33_333_300_000), total);
    }

    [Fact]
    public void Mixing_currencies_throws()
    {
        var rial = Money.Rial(1_000);
        var dollar = new Money(1_000, Currency.Usd);

        Assert.Throws<InvalidOperationException>(() => rial + dollar);
        Assert.Throws<InvalidOperationException>(() => rial - dollar);
        Assert.Throws<InvalidOperationException>(() => rial < dollar);
    }

    [Fact]
    public void Multiply_by_a_count_is_exact()
    {
        // The sample from the brief: 100 pieces issued, 92 accepted, paid per accepted piece.
        var ratePerPiece = Money.Rial(450_000);

        Assert.Equal(Money.Rial(41_400_000), ratePerPiece * 92);
    }

    [Theory]
    [InlineData(1_000, "2.5", 2_500)] // exact, nothing to round
    [InlineData(333, "0.5", 167)] // 166.5: a half rounds up
    [InlineData(335, "0.5", 168)] // 167.5: a half rounds up, not to the even 167
    [InlineData(999, "0.3334", 333)] // 333.0666 rounds down
    [InlineData(1_000, "0.3336", 334)] // 333.6 rounds up
    [InlineData(100_000, "1.4", 140_000)] // an overtime factor
    [InlineData(1, "0.4", 0)] // less than half a Rial disappears
    public void Multiply_by_a_fraction_rounds_half_away_from_zero(
        long amount, string factor, long expected)
    {
        var result = Money.Rial(amount) * decimal.Parse(factor, CultureInfo.InvariantCulture);

        Assert.Equal(Money.Rial(expected), result);
    }

    [Theory]
    [InlineData(333, "0.5")]
    [InlineData(335, "0.5")]
    [InlineData(999, "0.3334")]
    [InlineData(123_457, "0.125")]
    public void Rounding_a_negative_amount_mirrors_the_positive_one(long amount, string factor)
    {
        // A reversing entry must cancel the original exactly, to the last Rial.
        var parsedFactor = decimal.Parse(factor, CultureInfo.InvariantCulture);

        var original = Money.Rial(amount) * parsedFactor;
        var reversal = Money.Rial(-amount) * parsedFactor;

        Assert.True((original + reversal).IsZero);
    }

    [Fact]
    public void Overflow_throws_instead_of_wrapping_around()
    {
        var huge = Money.Rial(long.MaxValue);

        Assert.Throws<OverflowException>(() => huge + Money.Rial(1));
        Assert.Throws<OverflowException>(() => huge * 2);
        Assert.Throws<OverflowException>(() => huge * 1.5m);
    }

    [Fact]
    public void Amounts_in_the_same_currency_can_be_compared()
    {
        var small = Money.Rial(100);
        var large = Money.Rial(200);

        Assert.True(small < large);
        Assert.True(large > small);
        Assert.True(small <= Money.Rial(100));
        Assert.True(small >= Money.Rial(100));
    }

    [Fact]
    public void Sign_flags_describe_the_amount()
    {
        Assert.True(Money.Rial(1).IsPositive);
        Assert.True(Money.Rial(-1).IsNegative);
        Assert.True(Money.Rial(0).IsZero);
        Assert.False(Money.Rial(0).IsPositive);
        Assert.False(Money.Rial(0).IsNegative);
    }

    [Fact]
    public void Currency_is_found_by_its_code()
    {
        Assert.Equal(Currency.Irr, Currency.FromCode("IRR").Value);
        Assert.Equal(Currency.Usd, Currency.FromCode("USD").Value);
    }

    [Theory]
    [InlineData("IRT")] // Toman is not a currency the system holds
    [InlineData("irr")]
    [InlineData("")]
    [InlineData(null)]
    public void An_unknown_currency_code_is_a_validation_error(string? code)
    {
        var result = Currency.FromCode(code);

        Assert.True(result.IsFailure);
        Assert.Equal("currency.unknown", result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }
}
