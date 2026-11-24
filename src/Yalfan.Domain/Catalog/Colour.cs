using Yalfan.Domain.Common;

namespace Yalfan.Domain.Catalog;

/// <summary>
/// A colour from the one shared list: any colour can be used with any fabric (decided
/// 1405/09/01).
/// Its Persian name is unique, so the operator cannot enter the same one twice.
/// </summary>
public sealed class Colour : Entity
{
    public const int NameMaxLength = 50;

    private readonly List<NameTranslation> _names = [];

    private Colour()
    {
    }

    public IReadOnlyCollection<NameTranslation> Names => _names.AsReadOnly();

    /// <summary>
    /// An inactive item is left out of the pickers for new work; what already points at it keeps
    /// it (ADR 0022). There is no delete: models and variants point at it.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>The name the panel shows.</summary>
    public string PersianName => CatalogText.PersianOf(_names);

    public static Result<Colour> Create(string? name)
    {
        var persian = CatalogText.PersianName(name, NameMaxLength);
        if (persian.IsFailure)
        {
            return persian.Error;
        }

        var colour = new Colour { IsActive = true };
        colour._names.Add(new NameTranslation(Locale.Fa, persian.Value));
        return colour;
    }

    public Result Rename(string? name) => CatalogText.Rename(_names, name, NameMaxLength);

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;
}
