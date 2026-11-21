using Yalfan.Domain.Common;

namespace Yalfan.Domain.Catalog;

/// <summary>
/// A group of models in the store and in reports, such as backpacks. One level for now, at most one per model
/// (decided 1405/09/01): a parent level is one column later, when the store needs it.
/// Its Persian name is unique, so the operator cannot enter the same one twice.
/// </summary>
public sealed class Category : Entity
{
    public const int NameMaxLength = 50;

    private readonly List<NameTranslation> _names = [];

    private Category()
    {
    }

    public IReadOnlyCollection<NameTranslation> Names => _names.AsReadOnly();

    /// <summary>The name the panel shows.</summary>
    public string PersianName => CatalogText.PersianOf(_names);

    public static Result<Category> Create(string? name)
    {
        var persian = CatalogText.PersianName(name, NameMaxLength);
        if (persian.IsFailure)
        {
            return persian.Error;
        }

        var category = new Category();
        category._names.Add(new NameTranslation(Locale.Fa, persian.Value));
        return category;
    }

    public Result Rename(string? name) => CatalogText.Rename(_names, name, NameMaxLength);
}
