using EduCore.Data;

namespace EduCore.Models;

public class ClassSubjectTeacher : ISchoolScoped
{
    public Guid Id { get; set; }
    public Guid ClassId { get; set; }
    public Guid SubjectId { get; set; }
    public Guid TeacherId { get; set; }
    public Guid SchoolId { get; set; }

    // Null while this assignment is active. Ending it (rather than deleting) keeps every
    // DiaryEntry/Mark that referenced it valid, while still freeing (ClassId, SubjectId) up
    // for a new active assignment -- e.g. correcting a wrongly-assigned subject.
    public DateOnly? EndedOn { get; set; }
}
