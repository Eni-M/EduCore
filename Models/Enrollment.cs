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

    // Null while the enrollment is active. Set on transfer/withdrawal -- lets a student
    // re-enroll in the same class later without violating the active-enrollment uniqueness.
    public DateOnly? LeftAt { get; set; }

    // Set alongside LeftAt. Null on active enrollments, and on ones closed before the reason
    // was recorded.
    public EnrollmentExitReason? ExitReason { get; set; }
}
