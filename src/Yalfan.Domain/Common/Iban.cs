using System.Text;

namespace Yalfan.Domain.Common;

/// <summary>
/// An Iranian bank account number in IBAN form (شبا), always kept as <c>IR</c> followed by 24
/// digits. Only Iranian IBANs are accepted, because contractors and workers are paid inside Iran
/// (ADR 0019). Contractors and workers carry one; the payouts (Phase 15) pay to it.
/// <para>
/// An IBAN is a secret: it is stored encrypted, shown masked and audited without its value
/// (ADR 0019, ADR 0013). <see cref="ToString"/> therefore gives the masked form, so a log line or
/// an exception message that prints an IBAN by mistake does not leak it.
/// </para>
/// </summary>
public sealed record Iban
{
    public const string InvalidCode = "iban.invalid";

    /// <summary><c>IR</c> and 24 digits: two check digits and the 22-digit account number.</summary>
    public const int Length = 26;

    private const string CountryCode = "IR";

    private Iban(string value)
    {
        Value = value;
    }

    /// <summary>The whole number as <c>IRXXXXXXXXXXXXXXXXXXXXXXXX</c>. Read it only to pay or to show it to the owner.</summary>
    public string Value { get; }

    /// <summary>
    /// The form every screen shows: the shape of the number with only its last four digits, in
    /// groups of four like on a bank card (<c>IR** **** **** **** **** **12 34</c>).
    /// </summary>
    public string Masked
    {
        get
        {
            var hidden = new StringBuilder(Length + 6);
            for (var i = 0; i < Length; i++)
            {
                if (i > 0 && i % 4 == 0)
                {
                    hidden.Append(' ');
                }

                hidden.Append(i < CountryCode.Length || i >= Length - 4 ? Value[i] : '*');
            }

            return hidden.ToString();
        }
    }

    /// <summary>
    /// Accepts the forms people really type: with or without <c>IR</c> (the back of a bank card
    /// shows the 24 digits), lower or upper case, with spaces or dashes, in Persian, Arabic or
    /// Latin digits. The ISO 13616 check digits must be right, which catches a mistyped digit.
    /// </summary>
    public static Result<Iban> Create(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return Error.Validation(InvalidCode);
        }

        var compact = Digits.ToLatin(input)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .ToUpperInvariant();

        var digits = compact.StartsWith(CountryCode, StringComparison.Ordinal) ? compact[CountryCode.Length..] : compact;

        if (digits.Length != Length - CountryCode.Length || !Digits.AreAllLatin(digits))
        {
            return Error.Validation(InvalidCode);
        }

        var value = CountryCode + digits;
        return HasValidCheckDigits(value) ? new Iban(value) : Error.Validation(InvalidCode);
    }

    public override string ToString() => Masked;

    /// <summary>
    /// ISO 13616: move the country code and check digits to the end, turn letters into numbers
    /// (A = 10 … Z = 35), and the whole number modulo 97 must be 1. Computed digit by digit,
    /// because the number has 30 digits.
    /// </summary>
    private static bool HasValidCheckDigits(string iban)
    {
        var remainder = 0;
        foreach (var c in iban[4..] + iban[..4])
        {
            if (char.IsAsciiLetterUpper(c))
            {
                var letter = c - 'A' + 10;
                remainder = ((remainder * 100) + letter) % 97;
            }
            else
            {
                remainder = ((remainder * 10) + (c - '0')) % 97;
            }
        }

        return remainder == 1;
    }
}
