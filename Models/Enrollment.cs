namespace EduCore.Models;

public class Enrollment
{
    public Guid Id { get; private set; }
    public Guid StudentId { get; private set; }
    public Guid ClassId { get; private set; }
    public DateTime EnrolledAt { get; private set; }

    public Enrollment(Guid studentId, Guid classId)
    {
        Id = Guid.NewGuid();
        StudentId = studentId;
        ClassId = classId;
        EnrolledAt = DateTime.UtcNow;
    }
}