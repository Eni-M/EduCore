using EduCore.Data;

namespace EduCore.Models;

public class TeacherSubject : ISchoolScoped
{
    public Guid Id { get; set; }
    public Guid TeacherId { get; set; }
    public Guid SubjectId { get; set; }
    public Guid SchoolId { get; set; }
}
