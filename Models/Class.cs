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

    public int? Capacity { get; set; }

    // Which part of the school day this class runs in. Its timetable is built from
    // this shift's periods; null until a directorate assigns one.
    public Guid? ShiftId { get; set; }
    public SchoolShift? Shift { get; set; }
}
