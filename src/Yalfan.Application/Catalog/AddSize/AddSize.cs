using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Yalfan.Application.Common;
using Yalfan.Application.Common.Handlers;
using Yalfan.Domain.Catalog;
using Yalfan.Domain.Common;

namespace Yalfan.Application.Catalog.AddSize;

/// <summary>
/// One more size for an existing model (decided 1405/09/03). The first size of a model that had
/// none becomes the size of the bags it already has; a further size is given to every fabric and
/// colour the model still makes (ADR 0023).
/// </summary>
public sealed record AddSizeCommand(Guid ProductId, string? Name);

public sealed class AddSizeValidator : AbstractValidator<AddSizeCommand>
{
    public AddSizeValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(ProductSize.NameMaxLength);
    }
}

internal sealed class AddSizeHandler(IAppDbContext db) : ICommandHandler<AddSizeCommand, ProductDetails>
{
    public async Task<Result<ProductDetails>> Handle(AddSizeCommand command, CancellationToken cancellationToken)
    {
        var product = await ProductRecords.LoadAsync(db, command.ProductId, cancellationToken);
        if (product is null)
        {
            return CatalogErrors.ProductNotFound;
        }

        // A fabric or colour deactivated since is not used for new bags (ADR 0022), so the new
        // size is not made in it; the bags already in it stay.
        var fabricIds = product.Variants.Select(v => v.FabricId).Distinct().ToList();
        var activeFabrics = await db.Fabrics.AsNoTracking()
            .Where(f => fabricIds.Contains(f.Id) && f.IsActive)
            .Select(f => f.Id)
            .ToHashSetAsync(cancellationToken);

        var colourIds = product.Variants.Select(v => v.ColourId).Distinct().ToList();
        var activeColours = await db.Colours.AsNoTracking()
            .Where(c => colourIds.Contains(c.Id) && c.IsActive)
            .Select(c => c.Id)
            .ToHashSetAsync(cancellationToken);

        var added = product.AddSize(
            command.Name,
            (fabricId, colourId) => activeFabrics.Contains(fabricId) && activeColours.Contains(colourId));
        if (added.IsFailure)
        {
            return added.Error;
        }

        await db.SaveChangesAsync(cancellationToken);

        return await ProductRecords.DetailsAsync(db, product, cancellationToken);
    }
}
