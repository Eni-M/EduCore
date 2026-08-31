using EduCore.Data;

namespace EduCore.Models;

public class ClassSubjectTeacher : ISchoolScoped
{
    public Guid Id { get; set; }
    public Guid ClassId { get; set; }
    public Guid SubjectId { get; set; }
    public Guid TeacherId { get; set; }
    public Guid SchoolId { get; set; }

    public DateOnly? EndedOn { get; set; }
}
