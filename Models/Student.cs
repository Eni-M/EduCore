using EduCore.Data;
using EduCore.Enums;

namespace EduCore.Models;

public class Student : ISchoolScoped
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid SchoolId { get; set; }
    public DateOnly DateOfBirth { get; set; }
    public string PersonalNumber { get; set; } = null!;

    public Guid? CityId { get; set; }
    public City? City { get; set; }
    public DateOnly? GraduationDate { get; set; }

    public Gender? Gender { get; set; }

    public User User { get; set; } = null!;
    public School School { get; set; } = null!;
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public ICollection<Parent> Parents { get; set; } = new List<Parent>();
}
