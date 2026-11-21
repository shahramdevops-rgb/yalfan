using Yalfan.Domain.Common;

namespace Yalfan.Domain.Catalog;

/// <summary>
/// One size of one model. Sizes belong to each model, not to a shared list: the operator ticks
/// "has sizes", says how many, and types their names (decided 1405/09/01).
/// </summary>
public sealed class ProductSize : Entity
{
    public const int NameMaxLength = 30;

    private readonly List<NameTranslation> _names = [];

    private ProductSize()
    {
    }

    internal ProductSize(int position, string persianName)
    {
        Position = position;
        _names.Add(new NameTranslation(Locale.Fa, persianName));
    }

    /// <summary>Where the size stands in the model's list, from 1, in the order it was typed.</summary>
    public int Position { get; private set; }

    public IReadOnlyCollection<NameTranslation> Names => _names.AsReadOnly();

    public string PersianName => CatalogText.PersianOf(_names);
}
