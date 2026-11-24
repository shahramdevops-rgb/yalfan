using Microsoft.EntityFrameworkCore;
using Yalfan.Application.Common;
using Yalfan.Application.Common.Handlers;
using Yalfan.Domain.Catalog;
using Yalfan.Domain.Common;

namespace Yalfan.Application.Catalog.SetColourActive;

/// <summary>
/// Deactivates a colour, or activates it again. Inactive, it is left out of the pickers for
/// new work; what already points at it keeps it (ADR 0022).
/// </summary>
public sealed record SetColourActiveCommand(Guid Id, bool IsActive);

internal sealed class SetColourActiveHandler(IAppDbContext db) : ICommandHandler<SetColourActiveCommand, CatalogListItem>
{
    public async Task<Result<CatalogListItem>> Handle(SetColourActiveCommand command, CancellationToken cancellationToken)
    {
        var colour = await db.Colours.SingleOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (colour is null)
        {
            return CatalogErrors.ColourNotFound;
        }

        if (command.IsActive)
        {
            colour.Activate();
        }
        else
        {
            colour.Deactivate();
        }

        await db.SaveChangesAsync(cancellationToken);

        return new CatalogListItem(colour.Id, colour.PersianName, colour.IsActive);
    }
}
