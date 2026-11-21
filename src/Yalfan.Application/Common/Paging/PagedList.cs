namespace Yalfan.Application.Common.Paging;

/// <summary>One page of a long list, with the total so the screen can show how many pages there are.</summary>
public sealed record PagedList<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);

public static class Paging
{
    public const int DefaultPageSize = 20;

    /// <summary>Large enough for any screen, small enough that one request cannot read a whole table.</summary>
    public const int MaxPageSize = 100;
}
