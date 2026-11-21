using Yalfan.Domain.Common;

namespace Yalfan.Domain.Catalog;

/// <summary>
/// A kind of fabric a model can be sewn in. One model can be sewn in several fabrics, so the
/// fabric belongs to the variant, not to the model (decided 1405/09/01).
/// Its Persian name is unique, so the operator cannot enter the same one twice.
/// </summary>
public sealed class Fabric : Entity
{
    public const int NameMaxLength = 50;

    private readonly List<NameTranslation> _names = [];

    private Fabric()
    {
    }

    public IReadOnlyCollection<NameTranslation> Names => _names.AsReadOnly();

    /// <summary>The name the panel shows.</summary>
    public string PersianName => CatalogText.PersianOf(_names);

    public static Result<Fabric> Create(string? name)
    {
        var persian = CatalogText.PersianName(name, NameMaxLength);
        if (persian.IsFailure)
        {
            return persian.Error;
        }

        var fabric = new Fabric();
        fabric._names.Add(new NameTranslation(Locale.Fa, persian.Value));
        return fabric;
    }

    public Result Rename(string? name) => CatalogText.Rename(_names, name, NameMaxLength);
}
