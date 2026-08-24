using EduCore.Data;

namespace EduCore.Models;

public class Student : ISchoolScoped
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid SchoolId { get; set; }
    public DateOnly DateOfBirth { get; set; }
    public string PersonalNumber { get; set; } = null!;
    public DateOnly? GraduationDate { get; set; }

    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public ICollection<Parent> Parents { get; set; } = new List<Parent>();
}
