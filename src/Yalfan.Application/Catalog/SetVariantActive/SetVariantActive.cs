using Yalfan.Application.Common;
using Yalfan.Application.Common.Handlers;
using Yalfan.Domain.Catalog;
using Yalfan.Domain.Common;

namespace Yalfan.Application.Catalog.SetVariantActive;

/// <summary>
/// Switches one bag of a model on or off. The variant is looked up within the model named in the
/// address, so a mismatched address can never switch another model's bag.
/// </summary>
public sealed record SetVariantActiveCommand(Guid ProductId, Guid VariantId, bool IsActive);

internal sealed class SetVariantActiveHandler(IAppDbContext db) : ICommandHandler<SetVariantActiveCommand, ProductDetails>
{
    public async Task<Result<ProductDetails>> Handle(SetVariantActiveCommand command, CancellationToken cancellationToken)
    {
        var product = await ProductRecords.LoadAsync(db, command.ProductId, cancellationToken);
        if (product is null)
        {
            return CatalogErrors.ProductNotFound;
        }

        var changed = product.SetVariantActive(command.VariantId, command.IsActive);
        if (changed.IsFailure)
        {
            return changed.Error;
        }

        await db.SaveChangesAsync(cancellationToken);

        return await ProductRecords.DetailsAsync(db, product, cancellationToken);
    }
}
