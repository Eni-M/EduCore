namespace EduCore.Data;

// Resolves which school the current request is scoped to, for the global query filters
// in EduCoreDbContext. Null (e.g. no authenticated context) means "see nothing" rather
// than "see everything" -- cross-school/system-wide access must go through
// IQueryable.IgnoreQueryFilters() explicitly, so it's always an intentional, auditable choice.
public interface ICurrentSchoolAccessor
{
    Guid? CurrentSchoolId { get; }
}
