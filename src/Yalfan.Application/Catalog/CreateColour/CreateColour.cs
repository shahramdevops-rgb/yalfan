using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Yalfan.Application.Common;
using Yalfan.Application.Common.Handlers;
using Yalfan.Domain.Catalog;
using Yalfan.Domain.Common;

namespace Yalfan.Application.Catalog.CreateColour;

public sealed record CreateColourCommand(string? Name);

public sealed class CreateColourValidator : AbstractValidator<CreateColourCommand>
{
    public CreateColourValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(Colour.NameMaxLength);
    }
}

internal sealed class CreateColourHandler(IAppDbContext db) : ICommandHandler<CreateColourCommand, CatalogListItem>
{
    public async Task<Result<CatalogListItem>> Handle(CreateColourCommand command, CancellationToken cancellationToken)
    {
        var created = Colour.Create(command.Name);
        if (created.IsFailure)
        {
            return created.Error;
        }

        var colour = created.Value;
        var name = colour.PersianName;

        // Checked here for a coded answer; the unique index stops a duplicate that slips past at
        // the same moment.
        if (await db.Colours.AnyAsync(x => x.Names.Any(t => t.Locale == Locale.Fa && t.Name == name), cancellationToken))
        {
            return CatalogErrors.NameTaken;
        }

        db.Colours.Add(colour);
        await db.SaveChangesAsync(cancellationToken);

        return new CatalogListItem(colour.Id, name, colour.IsActive);
    }
}
