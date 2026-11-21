using Microsoft.EntityFrameworkCore;
using Yalfan.Application.Catalog;
using Yalfan.Infrastructure.Persistence;

namespace Yalfan.Infrastructure.Catalog;

/// <summary>Model numbers from the PostgreSQL sequence <see cref="Name"/> (ADR 0016).</summary>
internal sealed class ProductNumberSequence(AppDbContext db) : IProductNumbers
{
    public const string Name = "product_numbers";

    // nextval is outside any transaction: a number, once taken, is never handed out again, even
    // if the save that took it fails.
    public Task<int> NextAsync(CancellationToken cancellationToken) =>
        db.Database
            .SqlQuery<int>($"SELECT nextval('product_numbers')::int AS \"Value\"")
            .SingleAsync(cancellationToken);
}
