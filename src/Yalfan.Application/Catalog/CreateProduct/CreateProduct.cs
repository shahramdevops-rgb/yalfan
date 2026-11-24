using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Yalfan.Application.Common;
using Yalfan.Application.Common.Handlers;
using Yalfan.Domain.Catalog;
using Yalfan.Domain.Common;

namespace Yalfan.Application.Catalog.CreateProduct;

/// <summary>
/// A new model, with its sizes and its first variants in one request, so a half-entered model is
/// never saved. <see cref="Sizes"/> empty or missing means the model has no sizes.
/// </summary>
public sealed record CreateProductCommand(
    string? Name,
    string? Description,
    Guid? CategoryId,
    IReadOnlyList<string?>? Sizes,
    IReadOnlyList<NewVariant>? Variants);

/// <summary>
/// A variant of a model being created. <see cref="Size"/> is the name of one of the model's
/// sizes, as typed in the same request, because the sizes have no ids yet.
/// </summary>
public sealed record NewVariant(Guid? FabricId, Guid? ColourId, string? Size);

public sealed record CreateProductResponse(Guid Id, int Number);

public sealed class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
    /// <summary>More than any real model has; stops a runaway request, not the workshop.</summary>
    public const int MaxVariants = 500;

    public CreateProductValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(Product.NameMaxLength);
        RuleFor(command => command.Description).MaximumLength(Product.DescriptionMaxLength);
        RuleFor(command => command.CategoryId).NotEqual(Guid.Empty);

        RuleFor(command => command.Sizes).Must(sizes => sizes is null || sizes.Count <= Product.MaxSizes);
        RuleForEach(command => command.Sizes).NotEmpty().MaximumLength(ProductSize.NameMaxLength);

        RuleFor(command => command.Variants).Must(variants => variants is null || variants.Count <= MaxVariants);
        RuleForEach(command => command.Variants).ChildRules(variant =>
        {
            variant.RuleFor(v => v.FabricId).NotEmpty();
            variant.RuleFor(v => v.ColourId).NotEmpty();
        });
    }
}

internal sealed class CreateProductHandler(IAppDbContext db, IProductNumbers numbers, IClock clock)
    : ICommandHandler<CreateProductCommand, CreateProductResponse>
{
    public async Task<Result<CreateProductResponse>> Handle(CreateProductCommand command, CancellationToken cancellationToken)
    {
        var variants = command.Variants ?? [];

        var category = await CatalogReferences.CheckCategoryAsync(db, command.CategoryId, cancellationToken);
        if (category.IsFailure)
        {
            return category.Error;
        }

        var references = await CatalogReferences.CheckAsync(
            db,
            variants.Select(v => v.FabricId!.Value).ToList(),
            variants.Select(v => v.ColourId!.Value).ToList(),
            cancellationToken);
        if (references.IsFailure)
        {
            return references.Error;
        }

        if (await CatalogReferences.ProductNameTakenAsync(db, PersianText.Normalize(command.Name), exceptId: null, cancellationToken))
        {
            return CatalogErrors.NameTaken;
        }

        // Taken last, after every check that reads the database, so a refused request rarely
        // uses up a number.
        var number = await numbers.NextAsync(cancellationToken);

        var created = Product.Create(number, command.Name, command.Description, command.CategoryId, command.Sizes ?? [], clock.UtcNow);
        if (created.IsFailure)
        {
            return created.Error;
        }

        var product = created.Value;
        foreach (var variant in variants)
        {
            Guid? sizeId = null;
            if (!string.IsNullOrWhiteSpace(variant.Size))
            {
                // The same answer as adding a variant later: the model has no sizes at all.
                if (!product.HasSizes)
                {
                    return CatalogErrors.SizeNotAllowed;
                }

                var size = product.FindSize(variant.Size);
                if (size is null)
                {
                    return CatalogErrors.SizeNotFound;
                }

                sizeId = size.Id;
            }

            var added = product.AddVariant(variant.FabricId!.Value, variant.ColourId!.Value, sizeId);
            if (added.IsFailure)
            {
                return added.Error;
            }
        }

        db.Products.Add(product);
        await db.SaveChangesAsync(cancellationToken);

        return new CreateProductResponse(product.Id, product.Number);
    }
}
