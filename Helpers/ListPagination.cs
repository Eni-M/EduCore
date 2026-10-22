using EduCore.Configuration;
using EduCore.Dtos.Pagination;

namespace EduCore.Helpers;

public static class ListPagination
{
    // Single source of truth for page clamping: server-side paging (Skip/Take in the
    // database) must echo the same page/pageSize as the in-memory pager below.
    // The ceiling is a setting rather than a literal, but it still has to be reachable from
    // static helpers that no container hands anything to. Program.cs sets it once at start-up
    // from Pagination:MaxPageSize.
    public static int MaxPageSize { get; set; } = new PaginationOptions().MaxPageSize;

    public static (int Page, int PageSize) Normalize(PaginationQueryDto pagination) =>
        (Math.Max(1, pagination.Page), Math.Clamp(pagination.PageSize, 1, MaxPageSize));

    // Pages an already-materialised list for the HTTP response. This trims the payload
    // the client receives; it does not reduce the database read (the caller has already
    // loaded the full list). Push paging into the SQL query if a list ever grows large
    // enough that loading it in full is the bottleneck.
    //
    // Still used, deliberately, only for lists bounded by their scope: one school's academic
    // years, one year's classes, the states, cities and zones of one country, the countries,
    // the state admins, a teacher's or student's own subjects. Lists the outside world sizes --
    // students, guardians, schools, origin cities -- page in SQL.
    public static PagedResultDto<T> ToPagedResult<T>(
        this IEnumerable<T> source, PaginationQueryDto pagination)
    {
        var (page, pageSize) = Normalize(pagination);

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
