using EduCore.Dtos.Pagination;

namespace EduCore.Helpers;

public static class ListPagination
{
    // Pages an already-materialised list for the HTTP response. This trims the payload
    // the client receives; it does not reduce the database read (the caller has already
    // loaded the full list). Push paging into the SQL query if a list ever grows large
    // enough that loading it in full is the bottleneck.
    public static PagedResultDto<T> ToPagedResult<T>(
        this IEnumerable<T> source, PaginationQueryDto pagination)
    {
        var page = Math.Max(1, pagination.Page);
        var pageSize = Math.Clamp(pagination.PageSize, 1, 100);

        var list = source as IReadOnlyList<T> ?? source.ToList();
        var items = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new PagedResultDto<T>(items, page, pageSize, list.Count);
    }

    // Case-insensitive "any of these fields contains the term" filter over a materialised list.
    public static IEnumerable<T> Search<T>(
        this IEnumerable<T> source, string? search, Func<T, IEnumerable<string?>> fields)
    {
        if (string.IsNullOrWhiteSpace(search)) return source;

        var term = search.Trim();
        return source.Where(x =>
            fields(x).Any(f => f is not null && f.Contains(term, StringComparison.OrdinalIgnoreCase)));
    }
}
