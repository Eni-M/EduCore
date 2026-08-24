namespace EduCore.Models;

public class ClassSubjectTeacher
{
    public Guid Id { get; private set; }
    public Guid ClassId { get; private set; }
    public Guid SubjectId { get; private set; }
    public Guid TeacherId { get; private set; }

    public ClassSubjectTeacher(Guid classId, Guid subjectId, Guid teacherId)
    {
        Id = Guid.NewGuid();
        ClassId = classId;
        SubjectId = subjectId;
        TeacherId = teacherId;
    }
}
