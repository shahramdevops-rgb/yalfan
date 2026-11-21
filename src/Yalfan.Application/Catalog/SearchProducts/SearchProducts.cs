using System.Globalization;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Yalfan.Application.Common;
using Yalfan.Application.Common.Handlers;
using Yalfan.Application.Common.Paging;
using Yalfan.Domain.Common;

namespace Yalfan.Application.Catalog.SearchProducts;

/// <summary>
/// Models found by number or by part of the name, newest first, one page at a time: the workshop
/// has many models (answered 1405/09/01), too many for one list. An empty search lists them all.
/// </summary>
public sealed record SearchProductsQuery(string? Search, int? Page, int? PageSize);

public sealed record ProductListItem(
    Guid Id,
    int Number,
    string Name,
    CatalogOptionSummary? Category,
    int VariantCount,
    bool IsActive);

public sealed class SearchProductsValidator : AbstractValidator<SearchProductsQuery>
{
    public const int SearchMaxLength = 100;

    public SearchProductsValidator()
    {
        RuleFor(query => query.Search).MaximumLength(SearchMaxLength);
        RuleFor(query => query.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, Paging.MaxPageSize);
    }
}

internal sealed class SearchProductsHandler(IAppDbContext db)
    : IQueryHandler<SearchProductsQuery, PagedList<ProductListItem>>
{
    public async Task<Result<PagedList<ProductListItem>>> Handle(SearchProductsQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page ?? 1;
        var pageSize = query.PageSize ?? Paging.DefaultPageSize;

        var products = db.Products.AsNoTracking();

        var term = PersianText.Normalize(query.Search);
        if (term.Length > 0)
        {
            // "۱۰۳" typed on a Persian keyboard finds model 103 as well as names containing it.
            var number = int.TryParse(Digits.ToLatin(term), NumberStyles.None, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : -1;

            products = products.Where(p =>
                p.Number == number
                || p.Translations.Any(t => t.Locale == Locale.Fa && t.Name.Contains(term)));
        }

        var totalCount = await products.CountAsync(cancellationToken);

        var rows = await products
            .OrderByDescending(p => p.Number)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new
            {
                p.Id,
                p.Number,
                Name = p.Translations.Where(t => t.Locale == Locale.Fa).Select(t => t.Name).First(),
                p.CategoryId,
                VariantCount = p.Variants.Count,
                p.IsActive,
            })
            .ToListAsync(cancellationToken);

        var categoryIds = rows.Where(r => r.CategoryId is not null).Select(r => r.CategoryId!.Value).Distinct().ToList();
        var categories = await db.Categories.AsNoTracking()
            .Where(c => categoryIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => new CatalogOptionSummary(c.Id, c.PersianName), cancellationToken);

        var items = rows
            .Select(r => new ProductListItem(
                r.Id,
                r.Number,
                r.Name,
                r.CategoryId is { } categoryId ? categories[categoryId] : null,
                r.VariantCount,
                r.IsActive))
            .ToList();

        return new PagedList<ProductListItem>(items, totalCount, page, pageSize);
    }
}
