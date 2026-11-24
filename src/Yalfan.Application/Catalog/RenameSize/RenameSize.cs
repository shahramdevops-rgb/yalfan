using FluentValidation;
using Yalfan.Application.Common;
using Yalfan.Application.Common.Handlers;
using Yalfan.Domain.Catalog;
using Yalfan.Domain.Common;

namespace Yalfan.Application.Catalog.RenameSize;

/// <summary>Corrects the name of one of a model's sizes; the size is looked up within that model.</summary>
public sealed record RenameSizeCommand(Guid ProductId, Guid SizeId, string? Name);

public sealed class RenameSizeValidator : AbstractValidator<RenameSizeCommand>
{
    public RenameSizeValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(ProductSize.NameMaxLength);
    }
}

internal sealed class RenameSizeHandler(IAppDbContext db) : ICommandHandler<RenameSizeCommand, ProductDetails>
{
    public async Task<Result<ProductDetails>> Handle(RenameSizeCommand command, CancellationToken cancellationToken)
    {
        var product = await ProductRecords.LoadAsync(db, command.ProductId, cancellationToken);
        if (product is null)
        {
            return CatalogErrors.ProductNotFound;
        }

        var renamed = product.RenameSize(command.SizeId, command.Name);
        if (renamed.IsFailure)
        {
            return renamed.Error;
        }

        await db.SaveChangesAsync(cancellationToken);

        return await ProductRecords.DetailsAsync(db, product, cancellationToken);
    }
}
