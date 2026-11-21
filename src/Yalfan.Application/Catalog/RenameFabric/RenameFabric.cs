using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Yalfan.Application.Common;
using Yalfan.Application.Common.Handlers;
using Yalfan.Domain.Catalog;
using Yalfan.Domain.Common;

namespace Yalfan.Application.Catalog.RenameFabric;

public sealed record RenameFabricCommand(Guid Id, string? Name);

public sealed class RenameFabricValidator : AbstractValidator<RenameFabricCommand>
{
    public RenameFabricValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(Fabric.NameMaxLength);
    }
}

internal sealed class RenameFabricHandler(IAppDbContext db) : ICommandHandler<RenameFabricCommand, CatalogOptionSummary>
{
    public async Task<Result<CatalogOptionSummary>> Handle(RenameFabricCommand command, CancellationToken cancellationToken)
    {
        var fabric = await db.Fabrics.SingleOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (fabric is null)
        {
            return CatalogErrors.FabricNotFound;
        }

        var renamed = fabric.Rename(command.Name);
        if (renamed.IsFailure)
        {
            return renamed.Error;
        }

        var name = fabric.PersianName;
        if (await db.Fabrics.AnyAsync(
                x => x.Id != command.Id && x.Names.Any(t => t.Locale == Locale.Fa && t.Name == name),
                cancellationToken))
        {
            return CatalogErrors.NameTaken;
        }

        await db.SaveChangesAsync(cancellationToken);

        return new CatalogOptionSummary(fabric.Id, name);
    }
}
