using EduCore.Data;

namespace EduCore.Models;

public class Class : ISchoolScoped
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public Guid SchoolId { get; set; }
    public Guid AcademicYearId { get; set; }
    public int Grade { get; set; }
    public Guid? HomeroomTeacherId { get; set; }

    // Nullable: capacity-vs-actual is reported only for classes that declare one.
    public int? Capacity { get; set; }
}
