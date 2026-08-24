namespace EduCore.Models;

public class Class
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public Guid SchoolId { get; private set; }
    public int Grade { get; private set; }
    public Guid? HomeroomTeacherId { get; private set; }

    public Class(string name, Guid schoolId, int grade)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (grade < 1)
            throw new ArgumentOutOfRangeException(nameof(grade), "Grade must be at least 1.");

        Id = Guid.NewGuid();
        Name = name;
        SchoolId = schoolId;
        Grade = grade;
    }

    public void AssignHomeroomTeacher(Guid teacherId)
    {
        HomeroomTeacherId = teacherId;
    }
}