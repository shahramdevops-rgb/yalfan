using Microsoft.EntityFrameworkCore;
using Yalfan.Application.Common;
using Yalfan.Application.Common.Handlers;
using Yalfan.Domain.Catalog;
using Yalfan.Domain.Common;

namespace Yalfan.Application.Catalog.GetProduct;

public sealed record GetProductQuery(Guid Id);

internal sealed class GetProductHandler(IAppDbContext db) : IQueryHandler<GetProductQuery, ProductDetails>
{
    public async Task<Result<ProductDetails>> Handle(GetProductQuery query, CancellationToken cancellationToken)
    {
        var product = await db.Products
            .AsNoTracking()
            .Include(p => p.Sizes)
            .Include(p => p.Variants)
            .SingleOrDefaultAsync(p => p.Id == query.Id, cancellationToken);
        if (product is null)
        {
            return CatalogErrors.ProductNotFound;
        }

        return await ProductRecords.DetailsAsync(db, product, cancellationToken);
    }
}
