using Microsoft.EntityFrameworkCore;
using Yalfan.Application.Common;
using Yalfan.Application.Common.Handlers;
using Yalfan.Domain.Catalog;
using Yalfan.Domain.Common;

namespace Yalfan.Application.Catalog.SetCategoryActive;

/// <summary>
/// Deactivates a category, or activates it again. Inactive, it is left out of the pickers for
/// new work; what already points at it keeps it (ADR 0022).
/// </summary>
public sealed record SetCategoryActiveCommand(Guid Id, bool IsActive);

internal sealed class SetCategoryActiveHandler(IAppDbContext db) : ICommandHandler<SetCategoryActiveCommand, CatalogListItem>
{
    public async Task<Result<CatalogListItem>> Handle(SetCategoryActiveCommand command, CancellationToken cancellationToken)
    {
        var category = await db.Categories.SingleOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (category is null)
        {
            return CatalogErrors.CategoryNotFound;
        }

        if (command.IsActive)
        {
            category.Activate();
        }
        else
        {
            category.Deactivate();
        }

        await db.SaveChangesAsync(cancellationToken);

        return new CatalogListItem(category.Id, category.PersianName, category.IsActive);
    }
}
