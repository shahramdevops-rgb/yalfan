namespace Yalfan.Domain.Common;

/// <summary>
/// An amount of something in a unit: 92 pieces, 2.5 metres. The amount never has more decimal
/// places than its unit allows. A negative amount is valid: stock leaving a warehouse and
/// reversing movements need it.
/// </summary>
public sealed record Quantity : IComparable<Quantity>
{
    private Quantity(decimal amount, Unit unit)
    {
        Amount = amount;
        Unit = unit;
    }

    public decimal Amount { get; }

    public Unit Unit { get; }

    public bool IsZero => Amount == 0;

    public bool IsPositive => Amount > 0;

    public bool IsNegative => Amount < 0;

    /// <summary>
    /// Creates a quantity from typed input. Fails when the amount is finer than the unit
    /// allows, such as 2.5 pieces: the operator is told, the number is not silently rounded.
    /// </summary>
    public static Result<Quantity> Create(decimal amount, Unit unit)
    {
        ArgumentNullException.ThrowIfNull(unit);

        if (decimal.Round(amount, unit.DecimalPlaces) != amount)
        {
            return QuantityErrors.TooPrecise;
        }

        return new Quantity(amount, unit);
    }

    public static Quantity Pieces(long count) => new(count, Unit.Piece);

    public static Quantity Zero(Unit unit)
    {
        ArgumentNullException.ThrowIfNull(unit);
        return new Quantity(0, unit);
    }

    public Quantity Add(Quantity other)
    {
        RequireSameUnit(other);
        return new Quantity(Amount + other.Amount, Unit);
    }

    public Quantity Subtract(Quantity other)
    {
        RequireSameUnit(other);
        return new Quantity(Amount - other.Amount, Unit);
    }

    public Quantity Negate() => new(-Amount, Unit);

    /// <summary>
    /// Multiplies by a factor, such as a recipe's 0.35 metre per bag times 300 bags, and rounds
    /// to the unit's decimal places. A half rounds away from zero, the same rule as money.
    /// </summary>
    public Quantity Multiply(decimal factor) =>
        new(decimal.Round(Amount * factor, Unit.DecimalPlaces, MidpointRounding.AwayFromZero), Unit);

    public int CompareTo(Quantity? other)
    {
        if (other is null)
        {
            return 1;
        }

        RequireSameUnit(other);
        return Amount.CompareTo(other.Amount);
    }

    public static Quantity operator +(Quantity left, Quantity right) => left.Add(right);

    public static Quantity operator -(Quantity left, Quantity right) => left.Subtract(right);

    public static Quantity operator -(Quantity quantity) => quantity.Negate();

    public static Quantity operator *(Quantity quantity, decimal factor) => quantity.Multiply(factor);

    public static bool operator <(Quantity left, Quantity right) => left.CompareTo(right) < 0;

    public static bool operator >(Quantity left, Quantity right) => left.CompareTo(right) > 0;

    public static bool operator <=(Quantity left, Quantity right) => left.CompareTo(right) <= 0;

    public static bool operator >=(Quantity left, Quantity right) => left.CompareTo(right) >= 0;

    public override string ToString() => $"{Amount} {Unit.Code}";

    /// <summary>
    /// Mixing units is a bug in the calling code, not something a user can cause, so it throws.
    /// </summary>
    private void RequireSameUnit(Quantity other)
    {
        ArgumentNullException.ThrowIfNull(other);

        if (other.Unit != Unit)
        {
            throw new InvalidOperationException($"Cannot combine {Unit.Code} with {other.Unit.Code}.");
        }
    }
}

public static class QuantityErrors
{
    public static readonly Error TooPrecise = Error.Validation("quantity.too_precise");
}
