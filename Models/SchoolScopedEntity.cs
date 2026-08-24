using EduCore.Data;

namespace EduCore.Models;

public abstract class SchoolScopedEntity : ISchoolScoped
{
    public Guid Id { get; set; }
    public Guid SchoolId { get; set; }
}
