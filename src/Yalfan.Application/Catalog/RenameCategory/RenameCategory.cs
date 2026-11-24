using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Yalfan.Application.Common;
using Yalfan.Application.Common.Handlers;
using Yalfan.Domain.Catalog;
using Yalfan.Domain.Common;

namespace Yalfan.Application.Catalog.RenameCategory;

public sealed record RenameCategoryCommand(Guid Id, string? Name);

public sealed class RenameCategoryValidator : AbstractValidator<RenameCategoryCommand>
{
    public RenameCategoryValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(Category.NameMaxLength);
    }
}

internal sealed class RenameCategoryHandler(IAppDbContext db) : ICommandHandler<RenameCategoryCommand, CatalogListItem>
{
    public async Task<Result<CatalogListItem>> Handle(RenameCategoryCommand command, CancellationToken cancellationToken)
    {
        var category = await db.Categories.SingleOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (category is null)
        {
            return CatalogErrors.CategoryNotFound;
        }

        var renamed = category.Rename(command.Name);
        if (renamed.IsFailure)
        {
            return renamed.Error;
        }

        var name = category.PersianName;
        if (await db.Categories.AnyAsync(
                x => x.Id != command.Id && x.Names.Any(t => t.Locale == Locale.Fa && t.Name == name),
                cancellationToken))
        {
            return CatalogErrors.NameTaken;
        }

        await db.SaveChangesAsync(cancellationToken);

        return new CatalogListItem(category.Id, name, category.IsActive);
    }
}
