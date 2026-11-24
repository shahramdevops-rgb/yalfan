using FluentValidation;
using Yalfan.Application.Catalog.CreateProduct;
using Yalfan.Application.Common;
using Yalfan.Application.Common.Handlers;
using Yalfan.Domain.Catalog;
using Yalfan.Domain.Common;

namespace Yalfan.Application.Catalog.AddVariants;

/// <summary>
/// Adds bags to an existing model, all of them or none: the model's page ticks several colours
/// at once and sends one bag per colour and size, so a half-added set is never saved (ADR 0023).
/// </summary>
public sealed record AddVariantsCommand(Guid ProductId, IReadOnlyList<VariantToAdd>? Variants);

/// <summary>A bag to add: a fabric, a colour and, when the model has sizes, one of its sizes.</summary>
public sealed record VariantToAdd(Guid? FabricId, Guid? ColourId, Guid? SizeId);

public sealed class AddVariantsValidator : AbstractValidator<AddVariantsCommand>
{
    public AddVariantsValidator()
    {
        RuleFor(command => command.Variants)
            .NotEmpty()
            .Must(variants => variants!.Count <= CreateProductValidator.MaxVariants);
        RuleForEach(command => command.Variants).ChildRules(variant =>
        {
            variant.RuleFor(v => v.FabricId).NotEmpty();
            variant.RuleFor(v => v.ColourId).NotEmpty();
            variant.RuleFor(v => v.SizeId).NotEqual(Guid.Empty);
        });
    }
}

internal sealed class AddVariantsHandler(IAppDbContext db) : ICommandHandler<AddVariantsCommand, ProductDetails>
{
    public async Task<Result<ProductDetails>> Handle(AddVariantsCommand command, CancellationToken cancellationToken)
    {
        var product = await ProductRecords.LoadAsync(db, command.ProductId, cancellationToken);
        if (product is null)
        {
            return CatalogErrors.ProductNotFound;
        }

        var variants = command.Variants!;
        var references = await CatalogReferences.CheckAsync(
            db,
            variants.Select(v => v.FabricId!.Value).ToList(),
            variants.Select(v => v.ColourId!.Value).ToList(),
            cancellationToken);
        if (references.IsFailure)
        {
            return references.Error;
        }

        foreach (var variant in variants)
        {
            // A refusal returns before anything is saved, so none of the bags is added.
            var added = product.AddVariant(variant.FabricId!.Value, variant.ColourId!.Value, variant.SizeId);
            if (added.IsFailure)
            {
                return added.Error;
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        return await ProductRecords.DetailsAsync(db, product, cancellationToken);
    }
}
