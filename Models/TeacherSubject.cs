namespace EduCore.Models;

public class TeacherSubject
{
    public Guid Id { get; private set; }
    public Guid TeacherId { get; private set; }
    public Guid SubjectId { get; private set; }

    public TeacherSubject(Guid teacherId, Guid subjectId)
    {
        Id = Guid.NewGuid();
        TeacherId = teacherId;
        SubjectId = subjectId;
    }
}
