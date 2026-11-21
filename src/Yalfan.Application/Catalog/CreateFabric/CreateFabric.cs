using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Yalfan.Application.Common;
using Yalfan.Application.Common.Handlers;
using Yalfan.Domain.Catalog;
using Yalfan.Domain.Common;

namespace Yalfan.Application.Catalog.CreateFabric;

public sealed record CreateFabricCommand(string? Name);

public sealed class CreateFabricValidator : AbstractValidator<CreateFabricCommand>
{
    public CreateFabricValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(Fabric.NameMaxLength);
    }
}

internal sealed class CreateFabricHandler(IAppDbContext db) : ICommandHandler<CreateFabricCommand, CatalogOptionSummary>
{
    public async Task<Result<CatalogOptionSummary>> Handle(CreateFabricCommand command, CancellationToken cancellationToken)
    {
        var created = Fabric.Create(command.Name);
        if (created.IsFailure)
        {
            return created.Error;
        }

        var fabric = created.Value;
        var name = fabric.PersianName;

        // Checked here for a coded answer; the unique index stops a duplicate that slips past at
        // the same moment.
        if (await db.Fabrics.AnyAsync(x => x.Names.Any(t => t.Locale == Locale.Fa && t.Name == name), cancellationToken))
        {
            return CatalogErrors.NameTaken;
        }

        db.Fabrics.Add(fabric);
        await db.SaveChangesAsync(cancellationToken);

        return new CatalogOptionSummary(fabric.Id, name);
    }
}
