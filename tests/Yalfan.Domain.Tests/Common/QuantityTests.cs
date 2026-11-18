using System.Globalization;

namespace Yalfan.Domain.Tests.Common;

public class QuantityTests
{
    private static readonly Unit Metre = Unit.Create("metre", 2).Value;

    private static decimal Number(string text) => decimal.Parse(text, CultureInfo.InvariantCulture);

    [Fact]
    public void Pieces_creates_a_whole_number_of_pieces()
    {
        var quantity = Quantity.Pieces(92);

        Assert.Equal(92m, quantity.Amount);
        Assert.Equal(Unit.Piece, quantity.Unit);
    }

    [Theory]
    [InlineData("2.5")]
    [InlineData("0.01")]
    [InlineData("-3.75")]
    [InlineData("120")]
    public void An_amount_within_the_units_decimal_places_is_accepted(string amount)
    {
        var result = Quantity.Create(Number(amount), Metre);

        Assert.True(result.IsSuccess);
        Assert.Equal(Number(amount), result.Value.Amount);
    }

    [Fact]
    public void Half_a_piece_is_rejected_not_rounded()
    {
        var result = Quantity.Create(2.5m, Unit.Piece);

        Assert.True(result.IsFailure);
        Assert.Equal("quantity.too_precise", result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public void An_amount_finer_than_the_unit_is_rejected()
    {
        var result = Quantity.Create(2.555m, Metre);

        Assert.True(result.IsFailure);
        Assert.Equal(QuantityErrors.TooPrecise, result.Error);
    }

    [Fact]
    public void Trailing_zeros_do_not_change_a_quantity()
    {
        var typedWithZeros = Quantity.Create(92.00m, Unit.Piece);

        Assert.True(typedWithZeros.IsSuccess);
        Assert.Equal(Quantity.Pieces(92), typedWithZeros.Value);
        Assert.Equal(Quantity.Pieces(92).GetHashCode(), typedWithZeros.Value.GetHashCode());
    }

    [Fact]
    public void Add_and_subtract_keep_the_unit()
    {
        var issued = Quantity.Pieces(100);
        var delivered = Quantity.Pieces(92);

        Assert.Equal(Quantity.Pieces(192), issued + delivered);
        Assert.Equal(Quantity.Pieces(8), issued - delivered);
    }

    [Fact]
    public void Decimal_amounts_add_exactly()
    {
        var first = Quantity.Create(0.1m, Metre).Value;
        var second = Quantity.Create(0.2m, Metre).Value;

        Assert.Equal(Quantity.Create(0.3m, Metre).Value, first + second);
    }

    [Fact]
    public void A_quantity_plus_its_negation_is_zero()
    {
        var movement = Quantity.Create(12.5m, Metre).Value;

        var afterReversal = movement + movement.Negate();

        Assert.True(afterReversal.IsZero);
        Assert.Equal(Quantity.Zero(Metre), afterReversal);
    }

    [Fact]
    public void Mixing_units_throws()
    {
        var pieces = Quantity.Pieces(10);
        var metres = Quantity.Create(10m, Metre).Value;

        Assert.Throws<InvalidOperationException>(() => pieces + metres);
        Assert.Throws<InvalidOperationException>(() => pieces - metres);
        Assert.Throws<InvalidOperationException>(() => pieces > metres);
    }

    [Theory]
    [InlineData("0.35", "300", "105")] // a recipe: 0.35 m per bag, 300 bags
    [InlineData("1.25", "3", "3.75")] // exact
    [InlineData("0.35", "0.5", "0.18")] // 0.175: a half rounds up
    [InlineData("0.25", "0.5", "0.13")] // 0.125: a half rounds up, not to the even 0.12
    [InlineData("10", "0.3333", "3.33")] // 3.333 rounds down
    [InlineData("-0.35", "0.5", "-0.18")] // the mirror of the positive case
    public void Multiply_rounds_to_the_units_decimal_places(
        string amount, string factor, string expected)
    {
        var quantity = Quantity.Create(Number(amount), Metre).Value;

        var result = quantity * Number(factor);

        Assert.Equal(Quantity.Create(Number(expected), Metre).Value, result);
    }

    [Theory]
    [InlineData(10, "0.25", 3)] // 2.5 pieces: a half rounds up
    [InlineData(10, "0.24", 2)] // 2.4 pieces rounds down
    [InlineData(-10, "0.25", -3)]
    public void Multiplying_pieces_gives_whole_pieces(long count, string factor, long expected)
    {
        var result = Quantity.Pieces(count) * Number(factor);

        Assert.Equal(Quantity.Pieces(expected), result);
    }

    [Fact]
    public void Quantities_in_the_same_unit_can_be_compared()
    {
        var accepted = Quantity.Pieces(92);
        var issued = Quantity.Pieces(100);

        Assert.True(accepted < issued);
        Assert.True(issued > accepted);
        Assert.True(accepted <= Quantity.Pieces(92));
        Assert.True(accepted >= Quantity.Pieces(92));
    }

    [Fact]
    public void A_unit_code_is_trimmed_and_lower_cased()
    {
        var unit = Unit.Create("  Metre ", 2).Value;

        Assert.Equal("metre", unit.Code);
        Assert.Equal(2, unit.DecimalPlaces);
        Assert.Equal(Metre, unit);
    }

    [Theory]
    [InlineData(null, 0, "unit.code_required")]
    [InlineData("   ", 0, "unit.code_required")]
    [InlineData("a-unit-code-that-is-far-too-long", 0, "unit.code_too_long")]
    [InlineData("metre", -1, "unit.decimal_places_out_of_range")]
    [InlineData("metre", 7, "unit.decimal_places_out_of_range")]
    public void An_invalid_unit_is_a_validation_error(string? code, int decimalPlaces, string expected)
    {
        var result = Unit.Create(code, decimalPlaces);

        Assert.True(result.IsFailure);
        Assert.Equal(expected, result.Error.Code);
    }
}
