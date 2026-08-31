using EduCore.Data;
using EduCore.Enums;

namespace EduCore.Models;

public class Enrollment : ISchoolScoped
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid ClassId { get; set; }
    public Guid SchoolId { get; set; }
    public DateOnly EnrolledAt { get; set; }

    public DateOnly? LeftAt { get; set; }

    public EnrollmentExitReason? ExitReason { get; set; }
}
