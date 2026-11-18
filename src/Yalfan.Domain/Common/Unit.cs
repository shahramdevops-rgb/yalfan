namespace Yalfan.Domain.Common;

/// <summary>
/// A unit of measure and how finely it can be divided. A piece cannot be divided
/// (0 decimal places); a metre of fabric can (for example 2). The code is a stable identifier;
/// the word the operator sees comes from the panel's message file.
/// </summary>
public sealed record Unit
{
    public const int MaxCodeLength = 16;
    public const int MaxDecimalPlaces = 6;

    /// <summary>A whole piece: a bag, or a part of one. Work orders count in pieces.</summary>
    public static readonly Unit Piece = new("piece", 0);

    private Unit(string code, int decimalPlaces)
    {
        Code = code;
        DecimalPlaces = decimalPlaces;
    }

    public string Code { get; }

    public int DecimalPlaces { get; }

    /// <summary>
    /// Defines a unit. Which units the workshop uses (metre, roll, …) is decided in Phase 6;
    /// only <see cref="Piece"/> is fixed in code.
    /// </summary>
    public static Result<Unit> Create(string? code, int decimalPlaces)
    {
        var normalized = code?.Trim().ToLowerInvariant();

        if (string.IsNullOrEmpty(normalized))
        {
            return UnitErrors.CodeRequired;
        }

        if (normalized.Length > MaxCodeLength)
        {
            return UnitErrors.CodeTooLong;
        }

        if (decimalPlaces is < 0 or > MaxDecimalPlaces)
        {
            return UnitErrors.DecimalPlacesOutOfRange;
        }

        return new Unit(normalized, decimalPlaces);
    }

    public override string ToString() => Code;
}

public static class UnitErrors
{
    public static readonly Error CodeRequired = Error.Validation("unit.code_required");

    public static readonly Error CodeTooLong = Error.Validation("unit.code_too_long");

    public static readonly Error DecimalPlacesOutOfRange =
        Error.Validation("unit.decimal_places_out_of_range");
}
