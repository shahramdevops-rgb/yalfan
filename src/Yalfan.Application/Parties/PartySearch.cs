using Yalfan.Domain.Common;

namespace Yalfan.Application.Parties;

/// <summary>The search box of the contractor and worker lists: part of the name or of the mobile.</summary>
internal static class PartySearch
{
    public const int SearchMaxLength = 100;

    /// <summary>
    /// The digits to look for in the stored mobiles (<c>09XXXXXXXXX</c>), or null when the search
    /// is not a phone number. A whole number in any form (<c>+98…</c>, Persian digits) becomes the
    /// stored form; a part, such as the last four digits, is looked for as typed.
    /// </summary>
    public static string? MobileDigits(string term)
    {
        if (MobileNumber.Create(term) is { IsSuccess: true } whole)
        {
            return whole.Value.Value;
        }

        var digits = Digits.ToLatin(term).Replace(" ", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal);
        return Digits.AreAllLatin(digits) ? digits : null;
    }
}
