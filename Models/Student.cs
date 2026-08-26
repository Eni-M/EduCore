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
    public DateOnly? GraduationDate { get; set; }

    // Nullable because students created before gender was recorded genuinely have none --
    // defaulting them would invent data that then shows up in a government report. Required
    // for new students via StudentCreateDto.
    public Gender? Gender { get; set; }

    public User User { get; set; } = null!;
    public School School { get; set; } = null!;
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public ICollection<Parent> Parents { get; set; } = new List<Parent>();
}
