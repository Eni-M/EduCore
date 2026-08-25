namespace EduCore.Data;

// Marks an entity that belongs to exactly one school.
public interface ISchoolScoped
{
    Guid SchoolId { get; }
}
