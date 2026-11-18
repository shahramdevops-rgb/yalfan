namespace Yalfan.Domain.Common;

/// <summary>
/// An amount of money in one currency (ADR 0002). The amount is a whole number of the
/// currency's smallest unit: Rial for IRR, cents for USD. It is never a float, a double or a
/// fraction, so adding a million entries gives exactly the same total as adding them by hand.
/// A negative amount is valid: debts, reversing entries and balances need it.
/// </summary>
public sealed record Money : IComparable<Money>
{
    public Money(long amount, Currency currency)
    {
        ArgumentNullException.ThrowIfNull(currency);

        Amount = amount;
        Currency = currency;
    }

    public long Amount { get; }

    public Currency Currency { get; }

    public bool IsZero => Amount == 0;

    public bool IsPositive => Amount > 0;

    public bool IsNegative => Amount < 0;

    public static Money Rial(long amount) => new(amount, Currency.Irr);

    public static Money Zero(Currency currency) => new(0, currency);

    public Money Add(Money other)
    {
        RequireSameCurrency(other);
        return new Money(checked(Amount + other.Amount), Currency);
    }

    public Money Subtract(Money other)
    {
        RequireSameCurrency(other);
        return new Money(checked(Amount - other.Amount), Currency);
    }

    /// <summary>The same amount with the opposite sign: what a reversing entry carries.</summary>
    public Money Negate() => new(checked(-Amount), Currency);

    /// <summary>Multiplies by a whole number, such as a count of pieces. Always exact.</summary>
    public Money Multiply(long factor) => new(checked(Amount * factor), Currency);

    /// <summary>
    /// Multiplies by a number that may have a fraction, such as 2.5 metres or a 1.4 overtime
    /// factor, and rounds to a whole unit. A half rounds away from zero (0.5 becomes 1, -0.5
    /// becomes -1): it is what a person does by hand, and it makes the reversal of an amount
    /// the exact mirror of the amount.
    /// </summary>
    public Money Multiply(decimal factor)
    {
        var rounded = decimal.Round(Amount * factor, 0, MidpointRounding.AwayFromZero);
        return new Money(decimal.ToInt64(rounded), Currency);
    }

    public int CompareTo(Money? other)
    {
        if (other is null)
        {
            return 1;
        }

        RequireSameCurrency(other);
        return Amount.CompareTo(other.Amount);
    }

    public static Money operator +(Money left, Money right) => left.Add(right);

    public static Money operator -(Money left, Money right) => left.Subtract(right);

    public static Money operator -(Money money) => money.Negate();

    public static Money operator *(Money money, long factor) => money.Multiply(factor);

    public static Money operator *(Money money, decimal factor) => money.Multiply(factor);

    public static bool operator <(Money left, Money right) => left.CompareTo(right) < 0;

    public static bool operator >(Money left, Money right) => left.CompareTo(right) > 0;

    public static bool operator <=(Money left, Money right) => left.CompareTo(right) <= 0;

    public static bool operator >=(Money left, Money right) => left.CompareTo(right) >= 0;

    public override string ToString() => $"{Amount} {Currency.Code}";

    /// <summary>
    /// Mixing currencies is a bug in the calling code, not something a user can cause, so it
    /// throws. A use case that accepts a currency from outside checks it before doing arithmetic.
    /// </summary>
    private void RequireSameCurrency(Money other)
    {
        ArgumentNullException.ThrowIfNull(other);

        if (other.Currency != Currency)
        {
            throw new InvalidOperationException(
                $"Cannot combine {Currency.Code} with {other.Currency.Code}.");
        }
    }
}
