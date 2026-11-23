using Yalfan.Domain.Common;

namespace Yalfan.Domain.Production;

/// <summary>
/// One entry of the rate card: what an outside contractor is paid for one piece of a model at
/// one stage, from a start date on (ADR 0021). Contractors are paid by the piece; in-house
/// workers are on a day wage, which belongs to each worker (task 5.1), not here.
/// <para>
/// The rate card is append-only: an entry is never changed or deleted, so every property is
/// init-only and the database refuses UPDATE and DELETE. A new rate is a new entry with its own
/// start date; a mistyped rate is corrected by a new entry with the same start date, which
/// replaces it (<see cref="RateCard"/>).
/// </para>
/// </summary>
public sealed class PieceRate : Entity
{
    private PieceRate()
    {
    }

    private PieceRate(Guid productId, Guid stageId, Money amount, DateOnly startsOn, DateTimeOffset enteredAt)
    {
        ProductId = productId;
        StageId = stageId;
        Amount = amount;
        StartsOn = startsOn;
        EnteredAt = enteredAt;
    }

    /// <summary>The model. One rate covers all its fabrics, colours and sizes (decided 1405/09/02).</summary>
    public Guid ProductId { get; private init; }

    public Guid StageId { get; private init; }

    /// <summary>The pay for one piece, in Rial, more than zero. The same for every contractor.</summary>
    public Money Amount { get; private init; } = null!;

    /// <summary>
    /// The workshop's day from which the rate is paid. It may be in the past (a rate taken over
    /// from the paper book) or in the future (a raise announced for next month).
    /// </summary>
    public DateOnly StartsOn { get; private init; }

    /// <summary>When the entry was typed. Among entries with the same start date, the later one wins.</summary>
    public DateTimeOffset EnteredAt { get; private init; }

    public static Result<PieceRate> Create(
        Guid productId,
        Guid stageId,
        Money amount,
        DateOnly startsOn,
        DateTimeOffset enteredAt)
    {
        ArgumentNullException.ThrowIfNull(amount);

        if (productId == Guid.Empty || stageId == Guid.Empty)
        {
            throw new ArgumentException("A piece rate names a model and a stage.");
        }

        // The use case builds the amount from Rial; another currency is a bug, not a user mistake.
        if (amount.Currency != Currency.Irr)
        {
            throw new ArgumentException("A piece rate is paid in Rial (ADR 0002).", nameof(amount));
        }

        if (!amount.IsPositive)
        {
            return ProductionErrors.RateNotPositive;
        }

        return new PieceRate(productId, stageId, amount, startsOn, enteredAt);
    }
}
