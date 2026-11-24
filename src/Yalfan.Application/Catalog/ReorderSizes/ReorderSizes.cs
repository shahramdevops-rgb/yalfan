using FluentValidation;
using Yalfan.Application.Common;
using Yalfan.Application.Common.Handlers;
using Yalfan.Domain.Catalog;
using Yalfan.Domain.Common;

namespace Yalfan.Application.Catalog.ReorderSizes;

/// <summary>
/// A model's sizes in their new order, as the operator left them after dragging one: the whole
/// list, as for the stages (ADR 0022), so one made from an old copy is refused, not half applied.
/// </summary>
public sealed record ReorderSizesCommand(Guid ProductId, IReadOnlyList<Guid>? SizeIds);

public sealed class ReorderSizesValidator : AbstractValidator<ReorderSizesCommand>
{
    public ReorderSizesValidator()
    {
        RuleFor(command => command.SizeIds)
            .NotEmpty()
            .Must(ids => ids!.Count <= Product.MaxSizes)
            .Must(ids => ids!.Distinct().Count() == ids!.Count)
            .WithErrorCode(CatalogErrors.SizeOrderOutdated.Code);
    }
}

internal sealed class ReorderSizesHandler(IAppDbContext db) : ICommandHandler<ReorderSizesCommand, ProductDetails>
{
    public async Task<Result<ProductDetails>> Handle(ReorderSizesCommand command, CancellationToken cancellationToken)
    {
        var product = await ProductRecords.LoadAsync(db, command.ProductId, cancellationToken);
        if (product is null)
        {
            return CatalogErrors.ProductNotFound;
        }

        var reordered = product.ReorderSizes(command.SizeIds!);
        if (reordered.IsFailure)
        {
            return reordered.Error;
        }

        // Only the sizes whose place really changed are written, and each is audited.
        await db.SaveChangesAsync(cancellationToken);

        return await ProductRecords.DetailsAsync(db, product, cancellationToken);
    }
}
