namespace EduCore.Data;

// Names for the global query filters (EF Core 10 named filters). A bypass names the filter it
// skips -- IgnoreQueryFilters(QueryFilters.SchoolOnly) -- instead of switching off every filter at
// once, so a filter added later (soft delete, say) is not silently bypassed by old call sites.
public static class QueryFilters
{
    public const string School = "School";

    // What a bypass passes. An array field, not a collection literal: these calls sit inside
    // LINQ expression trees, which cannot hold a collection expression.
    public static readonly string[] SchoolOnly = [School];
}
