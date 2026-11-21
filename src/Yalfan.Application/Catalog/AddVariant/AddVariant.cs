using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Yalfan.Application.Common;
using Yalfan.Application.Common.Handlers;
using Yalfan.Domain.Catalog;
using Yalfan.Domain.Common;

namespace Yalfan.Application.Catalog.AddVariant;

/// <summary>Adds an existing model in one more fabric, colour and, when it has sizes, size.</summary>
public sealed record AddVariantCommand(Guid ProductId, Guid? FabricId, Guid? ColourId, Guid? SizeId);

public sealed record AddVariantResponse(Guid Id, string Sku);

public sealed class AddVariantValidator : AbstractValidator<AddVariantCommand>
{
    public AddVariantValidator()
    {
        RuleFor(command => command.FabricId).NotEmpty();
        RuleFor(command => command.ColourId).NotEmpty();
        RuleFor(command => command.SizeId).NotEqual(Guid.Empty);
    }
}

internal sealed class AddVariantHandler(IAppDbContext db) : ICommandHandler<AddVariantCommand, AddVariantResponse>
{
    public async Task<Result<AddVariantResponse>> Handle(AddVariantCommand command, CancellationToken cancellationToken)
    {
        var product = await db.Products
            .Include(p => p.Sizes)
            .Include(p => p.Variants)
            .SingleOrDefaultAsync(p => p.Id == command.ProductId, cancellationToken);
        if (product is null)
        {
            return CatalogErrors.ProductNotFound;
        }

        var fabricId = command.FabricId!.Value;
        var colourId = command.ColourId!.Value;

        var references = await CatalogReferences.CheckAsync(db, [fabricId], [colourId], cancellationToken);
        if (references.IsFailure)
        {
            return references.Error;
        }

        var added = product.AddVariant(fabricId, colourId, command.SizeId);
        if (added.IsFailure)
        {
            return added.Error;
        }

        await db.SaveChangesAsync(cancellationToken);

        return new AddVariantResponse(added.Value.Id, added.Value.Sku);
    }
}
