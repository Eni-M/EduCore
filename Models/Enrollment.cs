using EduCore.Data;
using EduCore.Enums;
using EduCore.Exceptions;

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

    // The one way an enrolment ends -- withdrawal and graduation both come through here, so the
    // rules live once instead of in each service that ends one.
    public void End(DateOnly leftAt, EnrollmentExitReason reason)
    {
        if (LeftAt is not null)
            throw new ConflictException("This enrollment has already been ended.");

        if (leftAt < EnrolledAt)
            throw new BusinessException("A student cannot leave a class before they joined it.");

        LeftAt = leftAt;
        ExitReason = reason;
    }
}
