using System.Text;

namespace Yalfan.Domain.Common;

/// <summary>
/// Persian names typed on different keyboards look the same but are different text: an Arabic
/// keyboard types «ي» and «ك» where a Persian one types «ی» and «ک». Names are normalized before
/// they are checked, stored or searched, so the same name typed twice is found as one name.
/// </summary>
public static class PersianText
{
    /// <summary>
    /// Trims, turns runs of white space into one space, and turns the Arabic forms of yeh and
    /// kaf into the Persian ones. Digits and the zero-width non-joiner are left as typed.
    /// </summary>
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var result = new StringBuilder(value.Length);
        var pendingSpace = false;

        foreach (var c in value.Trim())
        {
            if (char.IsWhiteSpace(c))
            {
                pendingSpace = true;
                continue;
            }

            if (pendingSpace)
            {
                result.Append(' ');
                pendingSpace = false;
            }

            result.Append(c switch
            {
                'ي' or 'ى' => 'ی',
                'ك' => 'ک',
                _ => c,
            });
        }

        return result.ToString();
    }
}
