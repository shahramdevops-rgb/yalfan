namespace Yalfan.Domain.Common;

/// <summary>
/// An Iranian mobile number, always kept as <c>09XXXXXXXXX</c>. Staff, contractors and store
/// customers log in with it and the login code goes to it by SMS through an Iranian line, so
/// only Iranian mobiles are accepted.
/// </summary>
public sealed record MobileNumber
{
    public const string InvalidCode = "mobile.invalid";

    private MobileNumber(string value)
    {
        Value = value;
    }

    /// <summary>The number as <c>09XXXXXXXXX</c>.</summary>
    public string Value { get; }

    /// <summary>
    /// Accepts the forms people really type: <c>09121234567</c>, <c>9121234567</c>,
    /// <c>+989121234567</c>, <c>00989121234567</c>, <c>989121234567</c>, with spaces or dashes,
    /// in Persian, Arabic or Latin digits.
    /// </summary>
    public static Result<MobileNumber> Create(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return Error.Validation(InvalidCode);
        }

        var digits = Digits.ToLatin(input)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace("(", string.Empty, StringComparison.Ordinal)
            .Replace(")", string.Empty, StringComparison.Ordinal);

        if (digits.StartsWith('+'))
        {
            digits = digits[1..];
        }

        var national = digits switch
        {
            { Length: 14 } when digits.StartsWith("0098", StringComparison.Ordinal) => digits[4..],
            { Length: 12 } when digits.StartsWith("98", StringComparison.Ordinal) => digits[2..],
            { Length: 11 } when digits.StartsWith('0') => digits[1..],
            { Length: 10 } => digits,
            _ => null,
        };

        if (national is null || national[0] != '9' || !Digits.AreAllLatin(national))
        {
            return Error.Validation(InvalidCode);
        }

        return new MobileNumber("0" + national);
    }

    public override string ToString() => Value;
}
