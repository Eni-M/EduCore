namespace EduCore.Models;

public class Teacher
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }

    public virtual ICollection<TeacherSubject> TeacherSubjects { get; set; } = new List<TeacherSubject>();

    public Teacher(Guid userId)
    {
        Id = Guid.NewGuid();
        UserId = userId;
    }
}
