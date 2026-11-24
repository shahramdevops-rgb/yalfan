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

    /// <summary>
    /// The model's sizes in their order, typed or dragged; empty when it has none. Two sizes added
    /// at the same moment can share a position, so the id breaks the tie (as for stages, ADR 0022).
    /// </summary>
    public IReadOnlyList<ProductSize> Sizes => _sizes.OrderBy(size => size.Position).ThenBy(size => size.Id).ToList();

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

        var persianDescription = Description(description);
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
        // Nothing new is made of a deactivated model (decided 1405/09/03, task 1.6.2).
        if (!IsActive)
        {
            return CatalogErrors.ProductInactive;
        }

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

    /// <summary>
    /// The name, description and category, as saved from the model's page. The number never
    /// changes: it is written on paper. The caller checks that the name is not another model's
    /// and that a newly chosen category exists and is active, because those are other rows.
    /// </summary>
    public Result Update(string? name, string? description, Guid? categoryId)
    {
        var persianName = CatalogText.PersianName(name, NameMaxLength);
        if (persianName.IsFailure)
        {
            return persianName.Error;
        }

        var persianDescription = Description(description);
        if (persianDescription?.Length > DescriptionMaxLength)
        {
            return CatalogErrors.DescriptionTooLong;
        }

        // Changed in place, so the audit records only the fields that really changed.
        PersianTranslation().Change(persianName.Value, persianDescription);
        CategoryId = categoryId;
        return Result.Success();
    }

    /// <summary>
    /// Takes the model out of use. Its variants keep their own switches, so activating the model
    /// again brings back exactly the bags that were in use; a variant is usable only while it and
    /// its model are both active.
    /// </summary>
    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;

    /// <summary>
    /// Switches one bag on or off: a fabric, colour or size that is no longer made. Never deleted,
    /// because work orders and stock will point at it, and its SKU is never given to another bag.
    /// </summary>
    public Result SetVariantActive(Guid variantId, bool isActive)
    {
        var variant = _variants.SingleOrDefault(v => v.Id == variantId);
        if (variant is null)
        {
            return CatalogErrors.VariantNotFound;
        }

        if (isActive)
        {
            variant.Activate();
        }
        else
        {
            variant.Deactivate();
        }

        return Result.Success();
    }

    /// <summary>
    /// One more size, at the end (decided 1405/09/03):
    /// <list type="bullet">
    /// <item>A model that had no sizes: every variant it already has becomes this size, because a
    /// model with sizes names a size on every variant and the bags made so far were of the size
    /// the operator adds first. Their SKUs stay, because they are written on paper.</item>
    /// <item>A model that had sizes: each fabric and colour it still makes (one of its bags in
    /// them is active) is made in the new size too, because a model is made in all its sizes.
    /// <paramref name="canBeMade"/> leaves out a fabric or colour that is no longer used for new
    /// work (ADR 0022); the caller knows which, because they are other rows.</item>
    /// </list>
    /// </summary>
    public Result<ProductSize> AddSize(string? name, Func<Guid, Guid, bool> canBeMade)
    {
        ArgumentNullException.ThrowIfNull(canBeMade);

        if (!IsActive)
        {
            return CatalogErrors.ProductInactive;
        }

        if (_sizes.Count >= MaxSizes)
        {
            return CatalogErrors.TooManySizes;
        }

        var persianSize = CatalogText.PersianName(name, ProductSize.NameMaxLength);
        if (persianSize.IsFailure)
        {
            return persianSize.Error;
        }

        if (_sizes.Any(size => size.PersianName == persianSize.Value))
        {
            return CatalogErrors.SizeNameDuplicate;
        }

        var hadSizes = HasSizes;
        var madePairs = Variants
            .Where(variant => variant.IsActive)
            .Select(variant => (variant.FabricId, variant.ColourId))
            .Distinct()
            .Where(pair => canBeMade(pair.FabricId, pair.ColourId))
            .ToList();

        var position = hadSizes ? _sizes.Max(size => size.Position) + 1 : 1;
        var newSize = new ProductSize(position, persianSize.Value);
        _sizes.Add(newSize);

        if (!hadSizes)
        {
            foreach (var variant in _variants)
            {
                variant.AssignSize(newSize.Id);
            }

            return newSize;
        }

        foreach (var (fabricId, colourId) in madePairs)
        {
            // Cannot be refused: the model is active, and no bag has the new size yet.
            var added = AddVariant(fabricId, colourId, newSize.Id);
            if (added.IsFailure)
            {
                throw new InvalidOperationException($"A new size could not be given to a bag: {added.Error.Code}.");
            }
        }

        return newSize;
    }

    /// <summary>Corrects a size's name; it stays unique within the model.</summary>
    public Result RenameSize(Guid sizeId, string? name)
    {
        var size = _sizes.SingleOrDefault(s => s.Id == sizeId);
        if (size is null)
        {
            return CatalogErrors.SizeNotFound;
        }

        var persianSize = CatalogText.PersianName(name, ProductSize.NameMaxLength);
        if (persianSize.IsFailure)
        {
            return persianSize.Error;
        }

        if (_sizes.Any(s => s.Id != sizeId && s.PersianName == persianSize.Value))
        {
            return CatalogErrors.SizeNameDuplicate;
        }

        size.Rename(persianSize.Value);
        return Result.Success();
    }

    /// <summary>
    /// Puts the sizes in the order the operator dragged them into, sent as the whole list as for
    /// the stages (ADR 0022). A list that does not name every size exactly once was made from an
    /// old copy, and changes nothing.
    /// </summary>
    public Result ReorderSizes(IReadOnlyList<Guid> orderedIds)
    {
        ArgumentNullException.ThrowIfNull(orderedIds);

        var byId = _sizes.ToDictionary(size => size.Id);
        if (orderedIds.Count != byId.Count
            || orderedIds.Distinct().Count() != orderedIds.Count
            || !orderedIds.All(byId.ContainsKey))
        {
            return CatalogErrors.SizeOrderOutdated;
        }

        for (var index = 0; index < orderedIds.Count; index++)
        {
            byId[orderedIds[index]].MoveTo(index + 1);
        }

        return Result.Success();
    }

    private static string? Description(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    private ProductTranslation PersianTranslation() => _translations.Single(t => t.Locale == Locale.Fa);
}
