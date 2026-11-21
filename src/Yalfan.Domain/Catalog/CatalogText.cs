using Yalfan.Domain.Common;

namespace Yalfan.Domain.Catalog;

/// <summary>The checks every Persian catalog name goes through, in one place.</summary>
internal static class CatalogText
{
    /// <summary>The normalized name, or why it cannot be used.</summary>
    public static Result<string> PersianName(string? value, int maxLength)
    {
        var name = PersianText.Normalize(value);

        if (name.Length == 0)
        {
            return CatalogErrors.NameRequired;
        }

        if (name.Length > maxLength)
        {
            return CatalogErrors.NameTooLong;
        }

        return name;
    }

    public static string PersianOf(IEnumerable<NameTranslation> names) =>
        names.Single(name => name.Locale == Locale.Fa).Name;

    /// <summary>Changes the Persian name in place, so the audit records it as one changed field.</summary>
    public static Result Rename(IEnumerable<NameTranslation> names, string? value, int maxLength)
    {
        var name = PersianName(value, maxLength);
        if (name.IsFailure)
        {
            return name.Error;
        }

        names.Single(n => n.Locale == Locale.Fa).Rename(name.Value);
        return Result.Success();
    }
}
