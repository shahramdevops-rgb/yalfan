using System.Globalization;
using Yalfan.Domain.Common;

namespace Yalfan.Domain.Catalog;

/// <summary>
/// A bag model: the design that is sewn, in one or more fabrics, colours and sizes (its
/// <see cref="Variants"/>). The workshop has no model codes, so the system numbers each model
/// (decided 1405/09/01); the number is written on work orders next to the name.
/// </summary>
public sealed class Product : Entity
{
    /// <summary>
    /// The first model number. Three digits from the start, because a one-digit number on a work
    /// order reads like a quantity (decided 1405/09/01).
    /// </summary>
    public const int FirstNumber = 101;

    public const int NameMaxLength = 100;

    public const int DescriptionMaxLength = 1000;

    public const int MaxSizes = 20;

    private readonly List<ProductTranslation> _translations = [];
    private readonly List<ProductSize> _sizes = [];
    private readonly List<Variant> _variants = [];

    private Product()
    {
    }

    private Product(int number, Guid? categoryId, DateTimeOffset createdAt)
    {
        Number = number;
        CategoryId = categoryId;
        IsActive = true;
        CreatedAt = createdAt;
    }

    /// <summary>The model number people read and write (101, 102, …); unique.</summary>
    public int Number { get; private set; }

    /// <summary>Optional for now: the store (Phase 11) is the first to need categories.</summary>
    public Guid? CategoryId { get; private set; }

    /// <summary>A model is never deleted, only deactivated: work orders will point at it.</summary>
    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyCollection<ProductTranslation> Translations => _translations.AsReadOnly();

    /// <summary>The model's sizes in the order they were typed; empty when it has none.</summary>
    public IReadOnlyList<ProductSize> Sizes => _sizes.OrderBy(size => size.Position).ToList();

    public IReadOnlyList<Variant> Variants => _variants.OrderBy(variant => variant.Number).ToList();

    public bool HasSizes => _sizes.Count > 0;

    public string PersianName => PersianTranslation().Name;

    public string? PersianDescription => PersianTranslation().Description;

    /// <summary>
    /// A new model with its Persian name and, when it has sizes, their names in order. An empty
    /// <paramref name="sizeNames"/> means the model has no sizes.
    /// </summary>
    public static Result<Product> Create(
        int number,
        string? name,
        string? description,
        Guid? categoryId,
        IReadOnlyList<string?> sizeNames,
        DateTimeOffset createdAt)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(number, 1);
        ArgumentNullException.ThrowIfNull(sizeNames);

        var persianName = CatalogText.PersianName(name, NameMaxLength);
        if (persianName.IsFailure)
        {
            return persianName.Error;
        }

        var persianDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        if (persianDescription?.Length > DescriptionMaxLength)
        {
            return CatalogErrors.DescriptionTooLong;
        }

        if (sizeNames.Count > MaxSizes)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeNames), $"A model has at most {MaxSizes} sizes.");
        }

        var product = new Product(number, categoryId, createdAt);
        product._translations.Add(new ProductTranslation(Locale.Fa, persianName.Value, persianDescription));

        foreach (var sizeName in sizeNames)
        {
            var persianSize = CatalogText.PersianName(sizeName, ProductSize.NameMaxLength);
            if (persianSize.IsFailure)
            {
                return persianSize.Error;
            }

            if (product._sizes.Any(size => size.PersianName == persianSize.Value))
            {
                return CatalogErrors.SizeNameDuplicate;
            }

            product._sizes.Add(new ProductSize(product._sizes.Count + 1, persianSize.Value));
        }

        return product;
    }

    /// <summary>
    /// Adds the model in one more fabric, colour and size. The fabric and colour must exist; the
    /// caller checks that, because they are other rows.
    /// </summary>
    public Result<Variant> AddVariant(Guid fabricId, Guid colourId, Guid? sizeId)
    {
        if (HasSizes && sizeId is null)
        {
            return CatalogErrors.SizeRequired;
        }

        if (!HasSizes && sizeId is not null)
        {
            return CatalogErrors.SizeNotAllowed;
        }

        if (sizeId is not null && _sizes.All(size => size.Id != sizeId))
        {
            return CatalogErrors.SizeNotFound;
        }

        if (_variants.Any(v => v.FabricId == fabricId && v.ColourId == colourId && v.SizeId == sizeId))
        {
            return CatalogErrors.VariantExists;
        }

        var variantNumber = _variants.Count == 0 ? 1 : _variants.Max(v => v.Number) + 1;
        var sku = string.Create(CultureInfo.InvariantCulture, $"{Number}-{variantNumber}");

        var variant = new Variant(variantNumber, sku, fabricId, colourId, sizeId);
        _variants.Add(variant);
        return variant;
    }

    /// <summary>The size with this Persian name, as the operator typed it when creating the model.</summary>
    public ProductSize? FindSize(string? persianName)
    {
        var name = PersianText.Normalize(persianName);
        return _sizes.SingleOrDefault(size => size.PersianName == name);
    }

    private ProductTranslation PersianTranslation() => _translations.Single(t => t.Locale == Locale.Fa);
}
