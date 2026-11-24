using Yalfan.Application.Common;
using Yalfan.Application.Common.Handlers;
using Yalfan.Domain.Catalog;
using Yalfan.Domain.Common;

namespace Yalfan.Application.Catalog.SetProductActive;

/// <summary>
/// Deactivates a model, or activates it again. There is no delete: work orders and stock will
/// point at it (ADR 0016). Its variants keep their own switches (ADR 0023).
/// </summary>
public sealed record SetProductActiveCommand(Guid Id, bool IsActive);

internal sealed class SetProductActiveHandler(IAppDbContext db) : ICommandHandler<SetProductActiveCommand, ProductDetails>
{
    public async Task<Result<ProductDetails>> Handle(SetProductActiveCommand command, CancellationToken cancellationToken)
    {
        var product = await ProductRecords.LoadAsync(db, command.Id, cancellationToken);
        if (product is null)
        {
            return CatalogErrors.ProductNotFound;
        }

        if (command.IsActive)
        {
            product.Activate();
        }
        else
        {
            product.Deactivate();
        }

        await db.SaveChangesAsync(cancellationToken);

        return await ProductRecords.DetailsAsync(db, product, cancellationToken);
    }
}
