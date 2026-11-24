using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Yalfan.Application.Common;
using Yalfan.Application.Common.Handlers;
using Yalfan.Application.Common.Paging;
using Yalfan.Application.Production;
using Yalfan.Domain.Common;

namespace Yalfan.Application.Parties.SearchContractors;

/// <summary>
/// Contractors found by part of the name or part of the mobile, in one search box like the
/// models, in Persian alphabetical order, one page at a time. <paramref name="IsActive"/> narrows
/// the list to active (the pickers of Phase 2) or inactive ones; empty lists both.
/// </summary>
public sealed record SearchContractorsQuery(string? Search, bool? IsActive, int? Page, int? PageSize);

public sealed record ContractorListItem(
    Guid Id,
    string FullName,
    string MobileNumber,
    IReadOnlyList<StageSummary> Stages,
    string? MaskedIban,
    bool IsActive);

public sealed class SearchContractorsValidator : AbstractValidator<SearchContractorsQuery>
{
    public SearchContractorsValidator()
    {
        RuleFor(query => query.Search).MaximumLength(PartySearch.SearchMaxLength);
        RuleFor(query => query.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, Paging.MaxPageSize);
    }
}

internal sealed class SearchContractorsHandler(IAppDbContext db)
    : IQueryHandler<SearchContractorsQuery, PagedList<ContractorListItem>>
{
    public async Task<Result<PagedList<ContractorListItem>>> Handle(SearchContractorsQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page ?? 1;
        var pageSize = query.PageSize ?? Paging.DefaultPageSize;

        var contractors = db.Contractors.AsNoTracking();

        if (query.IsActive is { } isActive)
        {
            contractors = contractors.Where(c => c.IsActive == isActive);
        }

        var term = PersianText.Normalize(query.Search);
        if (term.Length > 0)
        {
            var digits = PartySearch.MobileDigits(term);

            // The mobile is a value object stored as text through a converter; the cast through
            // object lets EF search the stored text for part of the number, which comparing value
            // objects cannot do.
            contractors = digits is null
                ? contractors.Where(c => c.FullName.Contains(term))
                : contractors.Where(c => c.FullName.Contains(term) || ((string)(object)c.MobileNumber).Contains(digits));
        }

        var totalCount = await contractors.CountAsync(cancellationToken);

        // The name column sorts in Persian alphabetical order (its collation); the id keeps the
        // pages stable when two contractors share a name.
        var found = await contractors
            .OrderBy(c => c.FullName)
            .ThenBy(c => c.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var stages = await PartyStages.InOrderAsync(db, found.SelectMany(c => c.StageIds), cancellationToken);

        var items = found
            .Select(c => new ContractorListItem(
                c.Id,
                c.FullName,
                c.MobileNumber.Value,
                PartyStages.Of(c.StageIds, stages),
                c.Iban?.Masked,
                c.IsActive))
            .ToList();

        return new PagedList<ContractorListItem>(items, totalCount, page, pageSize);
    }
}
