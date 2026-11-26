using Microsoft.EntityFrameworkCore;
using Yalfan.Application.Common;
using Yalfan.Application.Common.Handlers;
using Yalfan.Application.Production;
using Yalfan.Domain.Catalog;
using Yalfan.Domain.Common;
using static Yalfan.Application.History.AuditKeys;

namespace Yalfan.Application.History.GetModelHistory;

/// <summary>A model's change history, with its name, sizes, bags and rates (ADR 0027).</summary>
public sealed record GetModelHistoryQuery(Guid ProductId, DateTimeOffset? Before);

internal sealed class GetModelHistoryHandler(IAppDbContext db) : IQueryHandler<GetModelHistoryQuery, HistoryPage>
{
    public async Task<Result<HistoryPage>> Handle(GetModelHistoryQuery query, CancellationToken cancellationToken)
    {
        var product = await db.Products.AsNoTracking()
            .Include(p => p.Sizes)
            .Include(p => p.Variants)
            .SingleOrDefaultAsync(p => p.Id == query.ProductId, cancellationToken);
        if (product is null)
        {
            return CatalogErrors.ProductNotFound;
        }

        var rates = await db.PieceRates.AsNoTracking()
            .Where(rate => rate.ProductId == product.Id)
            .Select(rate => new { rate.Id, rate.StageId })
            .ToListAsync(cancellationToken);

        // Sizes, bags and rates are never deleted, so today's rows are every row the model ever
        // had. Only the Persian translation is read: the panel is Persian, and the store's other
        // languages join the history when they get a screen (Phase 11).
        var sizeKeys = product.Sizes.Select(size => Key(size.Id)).ToList();
        var page = await AuditTrail.ReadAsync(
            db,
            [
                new AuditedRows(AuditedTables.Products, [Key(product.Id)]),
                new AuditedRows(AuditedTables.ProductTranslations, [Persian(product.Id)]),
                new AuditedRows(AuditedTables.ProductSizes, sizeKeys),
                new AuditedRows(AuditedTables.ProductSizeTranslations, product.Sizes.Select(size => Persian(size.Id)).ToList()),
                new AuditedRows(AuditedTables.ProductVariants, product.Variants.Select(variant => Key(variant.Id)).ToList()),
                new AuditedRows(AuditedTables.PieceRates, rates.Select(rate => Key(rate.Id)).ToList()),
            ],
            query.Before,
            cancellationToken);

        var names = new HistoryNames();

        foreach (var size in product.Sizes)
        {
            names.Parts[Key(size.Id)] = new PartName(size.PersianName, size.Position);
            names.Ids[Key(size.Id)] = size.PersianName;
        }

        foreach (var variant in product.Variants)
        {
            names.Parts[Key(variant.Id)] = new PartName(variant.Sku, variant.Number);
        }

        var stages = await StageLists.InOrderAsync(db, cancellationToken);
        foreach (var rate in rates)
        {
            // Stages are never deleted, so the stage of a rate is always found.
            var stage = stages.FindIndex(s => s.Id == rate.StageId);
            names.Parts[Key(rate.Id)] = new PartName(stages[stage].Name, stage);
        }

        var categoryIds = page.IdsIn(AuditedTables.Products, "categoryId");
        foreach (var category in await db.Categories.AsNoTracking().Where(c => categoryIds.Contains(c.Id)).ToListAsync(cancellationToken))
        {
            names.Ids[Key(category.Id)] = category.PersianName;
        }

        var fabricIds = page.IdsIn(AuditedTables.ProductVariants, "fabricId");
        foreach (var fabric in await db.Fabrics.AsNoTracking().Where(f => fabricIds.Contains(f.Id)).ToListAsync(cancellationToken))
        {
            names.Ids[Key(fabric.Id)] = fabric.PersianName;
        }

        var colourIds = page.IdsIn(AuditedTables.ProductVariants, "colourId");
        foreach (var colour in await db.Colours.AsNoTracking().Where(c => colourIds.Contains(c.Id)).ToListAsync(cancellationToken))
        {
            names.Ids[Key(colour.Id)] = colour.PersianName;
        }

        return await HistoryAssembler.AssembleAsync(db, page, names, cancellationToken);
    }
}
