using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Yalfan.Application.Common;
using Yalfan.Application.Common.Handlers;
using Yalfan.Domain.Catalog;
using Yalfan.Domain.Common;

namespace Yalfan.Application.Catalog.CreateCategory;

public sealed record CreateCategoryCommand(string? Name);

public sealed class CreateCategoryValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(Category.NameMaxLength);
    }
}

internal sealed class CreateCategoryHandler(IAppDbContext db) : ICommandHandler<CreateCategoryCommand, CatalogOptionSummary>
{
    public async Task<Result<CatalogOptionSummary>> Handle(CreateCategoryCommand command, CancellationToken cancellationToken)
    {
        var created = Category.Create(command.Name);
        if (created.IsFailure)
        {
            return created.Error;
        }

        var category = created.Value;
        var name = category.PersianName;

        // Checked here for a coded answer; the unique index stops a duplicate that slips past at
        // the same moment.
        if (await db.Categories.AnyAsync(x => x.Names.Any(t => t.Locale == Locale.Fa && t.Name == name), cancellationToken))
        {
            return CatalogErrors.NameTaken;
        }

        db.Categories.Add(category);
        await db.SaveChangesAsync(cancellationToken);

        return new CatalogOptionSummary(category.Id, name);
    }
}
