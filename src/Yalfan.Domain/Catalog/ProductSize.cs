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

    /// <summary>
    /// Where the size stands in the model's list, from 1: the order it was typed in, or dragged
    /// into on the model's page. Not unique in the database, for the reason the stages' position
    /// is not (ADR 0022): a swap would fail half way.
    /// </summary>
    public int Position { get; private set; }

    public IReadOnlyCollection<NameTranslation> Names => _names.AsReadOnly();

    public string PersianName => CatalogText.PersianOf(_names);

    /// <summary>Changes the Persian name in place; the model has checked it is unique there.</summary>
    internal void Rename(string persianName) => _names.Single(name => name.Locale == Locale.Fa).Rename(persianName);

    internal void MoveTo(int position) => Position = position;
}
