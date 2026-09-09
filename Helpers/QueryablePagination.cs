using EduCore.Dtos.Pagination;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Helpers;

public static class QueryablePagination
{
    // One page of an already-ordered, already-filtered query, plus the total for the pager.
    // Callers order first: skip/take on an unordered query is not stable.
    public static async Task<(IReadOnlyList<T> Items, int TotalCount)> ToPageAsync<T>(
        this IQueryable<T> query, PaginationQueryDto pagination, CancellationToken ct)
    {
        var page = Math.Max(1, pagination.Page);
        var pageSize = Math.Clamp(pagination.PageSize, 1, ListPagination.MaxPageSize);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }
}
