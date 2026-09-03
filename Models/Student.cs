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

    // Student origin — where the family comes from. Separate from school geography;
    // a student may be from outside Kosovo.
    public Guid? CountryId { get; set; }
    public Country? Country { get; set; }
    public Guid? OriginCityId { get; set; }
    public OriginCity? OriginCity { get; set; }
    public string? Address { get; set; }

    public DateOnly? GraduationDate { get; set; }

    public Gender? Gender { get; set; }

    public User User { get; set; } = null!;
    public School School { get; set; } = null!;
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public ICollection<GuardianStudent> Guardians { get; set; } = new List<GuardianStudent>();
}
