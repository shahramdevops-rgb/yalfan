using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Yalfan.Application.Common;
using Yalfan.Application.Common.Handlers;
using Yalfan.Application.Common.Paging;
using Yalfan.Application.Production;
using Yalfan.Domain.Common;

namespace Yalfan.Application.Parties.SearchWorkers;

/// <summary>
/// Workers found by part of the name or part of the mobile, in one search box like the
/// contractors, in Persian alphabetical order, one page at a time. <paramref name="IsActive"/>
/// narrows the list to active or inactive workers; empty lists both.
/// </summary>
public sealed record SearchWorkersQuery(string? Search, bool? IsActive, int? Page, int? PageSize);

public sealed record WorkerListItem(
    Guid Id,
    string FullName,
    string MobileNumber,
    IReadOnlyList<StageSummary> Stages,
    DateOnly StartDate,
    string? MaskedIban,
    bool IsActive);

public sealed class SearchWorkersValidator : AbstractValidator<SearchWorkersQuery>
{
    public SearchWorkersValidator()
    {
        RuleFor(query => query.Search).MaximumLength(PartySearch.SearchMaxLength);
        RuleFor(query => query.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, Paging.MaxPageSize);
    }
}

internal sealed class SearchWorkersHandler(IAppDbContext db)
    : IQueryHandler<SearchWorkersQuery, PagedList<WorkerListItem>>
{
    public async Task<Result<PagedList<WorkerListItem>>> Handle(SearchWorkersQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page ?? 1;
        var pageSize = query.PageSize ?? Paging.DefaultPageSize;

        var workers = db.Workers.AsNoTracking();

        if (query.IsActive is { } isActive)
        {
            workers = workers.Where(w => w.IsActive == isActive);
        }

        var term = PersianText.Normalize(query.Search);
        if (term.Length > 0)
        {
            var digits = PartySearch.MobileDigits(term);

            // The cast through object lets EF search the stored text of the mobile value object
            // for part of the number, as in the contractor search.
            workers = digits is null
                ? workers.Where(w => w.FullName.Contains(term))
                : workers.Where(w => w.FullName.Contains(term) || ((string)(object)w.MobileNumber).Contains(digits));
        }

        var totalCount = await workers.CountAsync(cancellationToken);

        // The name column sorts in Persian alphabetical order (its collation); the id keeps the
        // pages stable when two workers share a name.
        var found = await workers
            .OrderBy(w => w.FullName)
            .ThenBy(w => w.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var stages = await PartyStages.InOrderAsync(db, found.SelectMany(w => w.StageIds), cancellationToken);

        var items = found
            .Select(w => new WorkerListItem(
                w.Id,
                w.FullName,
                w.MobileNumber.Value,
                PartyStages.Of(w.StageIds, stages),
                w.StartDate,
                w.Iban?.Masked,
                w.IsActive))
            .ToList();

        return new PagedList<WorkerListItem>(items, totalCount, page, pageSize);
    }
}
