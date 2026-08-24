namespace EduCore.Models;

public class Student
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public DateOnly DateOfBirth { get; private set; }
    public string PersonalNumber { get; private set; } = null!;
    public DateOnly? GraduationDate { get; private set; }

    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public virtual ICollection<Parent> Parents { get; set; } = new List<Parent>();

    public Student(Guid userId, DateOnly dateOfBirth, string personalNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(personalNumber);

        Id = Guid.NewGuid();
        UserId = userId;
        DateOfBirth = dateOfBirth;
        PersonalNumber = personalNumber;
    }

    public void Graduate(DateOnly graduationDate)
    {
        GraduationDate = graduationDate;
    }
}
