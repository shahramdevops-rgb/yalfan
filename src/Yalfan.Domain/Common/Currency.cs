namespace Yalfan.Domain.Common;

/// <summary>
/// A currency the system can hold money in, by its ISO 4217 code. Only the listed currencies
/// exist, so a typo or the Toman pseudo-code "IRT" can never slip into a ledger.
/// </summary>
public sealed record Currency
{
    /// <summary>Iranian Rial. Every amount inside Iran is held in Rial (ADR 0002).</summary>
    public static readonly Currency Irr = new("IRR");

    /// <summary>
    /// US Dollar, held in cents. Present for proformas to foreign buyers; which currencies the
    /// price lists really use is decided in Phase 8.
    /// </summary>
    public static readonly Currency Usd = new("USD");

    private static readonly Currency[] Supported = [Irr, Usd];

    private Currency(string code)
    {
        Code = code;
    }

    public string Code { get; }

    public static Result<Currency> FromCode(string? code)
    {
        var currency = Supported.FirstOrDefault(supported => supported.Code == code);

        return currency is null ? CurrencyErrors.Unknown : currency;
    }

    public override string ToString() => Code;
}

public static class CurrencyErrors
{
    public static readonly Error Unknown = Error.Validation("currency.unknown");
}
