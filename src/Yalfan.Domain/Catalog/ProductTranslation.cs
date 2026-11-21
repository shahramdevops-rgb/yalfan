using Yalfan.Domain.Common;

namespace Yalfan.Domain.Catalog;

/// <summary>
/// A model's name and description in one language. Product text lives in these rows, never in a
/// single Persian column (ADR 0006). The panel fills only Persian; where Arabic and English are
/// entered is decided in Phase 11.
/// </summary>
public sealed class ProductTranslation
{
    private ProductTranslation()
    {
    }

    internal ProductTranslation(Locale locale, string name, string? description)
    {
        Locale = locale;
        Name = name;
        Description = description;
    }

    public Locale Locale { get; private set; }

    public string Name { get; private set; } = null!;

    public string? Description { get; private set; }
}
