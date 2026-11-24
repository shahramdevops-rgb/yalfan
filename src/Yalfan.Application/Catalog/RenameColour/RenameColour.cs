using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Yalfan.Application.Common;
using Yalfan.Application.Common.Handlers;
using Yalfan.Domain.Catalog;
using Yalfan.Domain.Common;

namespace Yalfan.Application.Catalog.RenameColour;

public sealed record RenameColourCommand(Guid Id, string? Name);

public sealed class RenameColourValidator : AbstractValidator<RenameColourCommand>
{
    public RenameColourValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(Colour.NameMaxLength);
    }
}

internal sealed class RenameColourHandler(IAppDbContext db) : ICommandHandler<RenameColourCommand, CatalogListItem>
{
    public async Task<Result<CatalogListItem>> Handle(RenameColourCommand command, CancellationToken cancellationToken)
    {
        var colour = await db.Colours.SingleOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (colour is null)
        {
            return CatalogErrors.ColourNotFound;
        }

        var renamed = colour.Rename(command.Name);
        if (renamed.IsFailure)
        {
            return renamed.Error;
        }

        var name = colour.PersianName;
        if (await db.Colours.AnyAsync(
                x => x.Id != command.Id && x.Names.Any(t => t.Locale == Locale.Fa && t.Name == name),
                cancellationToken))
        {
            return CatalogErrors.NameTaken;
        }

        await db.SaveChangesAsync(cancellationToken);

        return new CatalogListItem(colour.Id, name, colour.IsActive);
    }
}
