namespace Yalfan.Domain.Common;

/// <summary>
/// People type numbers on Persian or Arabic keyboards; input is normalized to Latin digits
/// before it is checked or stored (ARCHITECTURE.md 10).
/// </summary>
public static class Digits
{
    /// <summary>Turns Persian (۰–۹) and Arabic-Indic (٠–٩) digits into 0–9; leaves the rest as is.</summary>
    public static string ToLatin(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return string.Create(value.Length, value, static (span, source) =>
        {
            for (var i = 0; i < source.Length; i++)
            {
                var c = source[i];
                span[i] = c switch
                {
                    >= '۰' and <= '۹' => (char)('0' + (c - '۰')),
                    >= '٠' and <= '٩' => (char)('0' + (c - '٠')),
                    _ => c,
                };
            }
        });
    }

    /// <summary>True when the value is only the Latin digits 0–9 and is not empty.</summary>
    public static bool AreAllLatin(string value) =>
        value.Length > 0 && value.All(char.IsAsciiDigit);
}
