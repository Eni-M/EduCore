namespace EduCore.Data;

// Marker for every entity that belongs to exactly one school. Implemented by all the
// school-scoped models so SchoolIsolationSaveChangesInterceptor can find them generically.
public interface ISchoolScoped
{
    Guid SchoolId { get; }
}
