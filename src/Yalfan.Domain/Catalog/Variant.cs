using Yalfan.Domain.Common;

namespace Yalfan.Domain.Catalog;

/// <summary>
/// One bag that can be made and stocked: a model in one fabric, one colour and, when the model
/// has sizes, one size (decided 1405/09/01). Work orders and stock point at variants, so a
/// variant is never deleted, only deactivated.
/// </summary>
public sealed class Variant : Entity
{
    public const int SkuMaxLength = 20;

    private Variant()
    {
    }

    internal Variant(int number, string sku, Guid fabricId, Guid colourId, Guid? sizeId)
    {
        Number = number;
        Sku = sku;
        FabricId = fabricId;
        ColourId = colourId;
        SizeId = sizeId;
        IsActive = true;
    }

    /// <summary>The variant's place within its model, from 1, in the order variants were added.</summary>
    public int Number { get; private set; }

    /// <summary>
    /// Model number and variant number, such as <c>101-3</c>. Made by the system, because the
    /// workshop has no codes and the operator should not invent them (decided 1405/09/01).
    /// </summary>
    public string Sku { get; private set; } = null!;

    public Guid FabricId { get; private set; }

    public Guid ColourId { get; private set; }

    /// <summary>One of the model's sizes, or empty when the model has none.</summary>
    public Guid? SizeId { get; private set; }

    public bool IsActive { get; private set; }

    internal void Activate() => IsActive = true;

    internal void Deactivate() => IsActive = false;

    /// <summary>
    /// Gives a size to a bag of a model that had none, when the model gets its first size; the
    /// SKU stays. Only <see cref="Product.AddSize"/> calls it.
    /// </summary>
    internal void AssignSize(Guid sizeId)
    {
        if (SizeId is not null)
        {
            throw new InvalidOperationException("A variant that has a size keeps it.");
        }

        SizeId = sizeId;
    }
}
