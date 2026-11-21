using Yalfan.Domain.Common;

namespace Yalfan.Domain.Catalog;

/// <summary>
/// The name of a category, fabric, colour or size in one language. Names live in these rows,
/// never in a single Persian column, because the store shows them in three languages (ADR 0006).
/// The panel fills only Persian.
/// </summary>
public sealed class NameTranslation
{
    private NameTranslation()
    {
    }

    internal NameTranslation(Locale locale, string name)
    {
        Locale = locale;
        Name = name;
    }

    public Locale Locale { get; private set; }

    public string Name { get; private set; } = null!;

    internal void Rename(string name) => Name = name;
}
