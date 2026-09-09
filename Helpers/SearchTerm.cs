namespace EduCore.Helpers;

public static class SearchTerm
{
    // The ILIKE pattern for a search box, or null when there is nothing to search for.
    // Repositories pattern-match on the result so a blank search adds no WHERE clause:
    //
    //     if (SearchTerm.Like(search) is { } term)
    //         query = query.Where(x => EF.Functions.ILike(x.Name, term));
    public static string? Like(string? search) =>
        string.IsNullOrWhiteSpace(search) ? null : $"%{search.Trim()}%";
}
