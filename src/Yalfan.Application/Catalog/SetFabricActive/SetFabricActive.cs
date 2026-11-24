using Microsoft.EntityFrameworkCore;
using Yalfan.Application.Common;
using Yalfan.Application.Common.Handlers;
using Yalfan.Domain.Catalog;
using Yalfan.Domain.Common;

namespace Yalfan.Application.Catalog.SetFabricActive;

/// <summary>
/// Deactivates a fabric, or activates it again. Inactive, it is left out of the pickers for
/// new work; what already points at it keeps it (ADR 0022).
/// </summary>
public sealed record SetFabricActiveCommand(Guid Id, bool IsActive);

internal sealed class SetFabricActiveHandler(IAppDbContext db) : ICommandHandler<SetFabricActiveCommand, CatalogListItem>
{
    public async Task<Result<CatalogListItem>> Handle(SetFabricActiveCommand command, CancellationToken cancellationToken)
    {
        var fabric = await db.Fabrics.SingleOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (fabric is null)
        {
            return CatalogErrors.FabricNotFound;
        }

        if (command.IsActive)
        {
            fabric.Activate();
        }
        else
        {
            fabric.Deactivate();
        }

        await db.SaveChangesAsync(cancellationToken);

        return new CatalogListItem(fabric.Id, fabric.PersianName, fabric.IsActive);
    }
}
