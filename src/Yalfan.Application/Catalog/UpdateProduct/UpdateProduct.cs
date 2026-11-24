using FluentValidation;
using Yalfan.Application.Common;
using Yalfan.Application.Common.Handlers;
using Yalfan.Domain.Catalog;
using Yalfan.Domain.Common;

namespace Yalfan.Application.Catalog.UpdateProduct;

/// <summary>
/// The model's name, description and category, saved from its page. The page edits one of them
/// at a time and sends the other two as they are, the way a contractor's profile is saved.
/// </summary>
public sealed record UpdateProductCommand(Guid Id, string? Name, string? Description, Guid? CategoryId);

public sealed class UpdateProductValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(Product.NameMaxLength);
        RuleFor(command => command.Description).MaximumLength(Product.DescriptionMaxLength);
        RuleFor(command => command.CategoryId).NotEqual(Guid.Empty);
    }
}

internal sealed class UpdateProductHandler(IAppDbContext db) : ICommandHandler<UpdateProductCommand, ProductDetails>
{
    public async Task<Result<ProductDetails>> Handle(UpdateProductCommand command, CancellationToken cancellationToken)
    {
        var product = await ProductRecords.LoadAsync(db, command.Id, cancellationToken);
        if (product is null)
        {
            return CatalogErrors.ProductNotFound;
        }

        // Only a newly chosen category must be active: a category deactivated after the model
        // was put in it stays, so saving the name never fails on it (as a contractor's stages,
        // ADR 0022).
        if (command.CategoryId != product.CategoryId)
        {
            var category = await CatalogReferences.CheckCategoryAsync(db, command.CategoryId, cancellationToken);
            if (category.IsFailure)
            {
                return category.Error;
            }
        }

        var updated = product.Update(command.Name, command.Description, command.CategoryId);
        if (updated.IsFailure)
        {
            return updated.Error;
        }

        if (await CatalogReferences.ProductNameTakenAsync(db, product.PersianName, product.Id, cancellationToken))
        {
            return CatalogErrors.NameTaken;
        }

        await db.SaveChangesAsync(cancellationToken);

        return await ProductRecords.DetailsAsync(db, product, cancellationToken);
    }
}
