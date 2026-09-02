using EduCore.Enums;

namespace EduCore.Security;

public interface ICurrentUser
{
    Guid UserId { get; }
    UserRole? Role { get; }

    Guid? SchoolId { get; }
    Guid? TeacherId { get; }
    Guid? StudentId { get; }
    Guid? CityId { get; }
    Guid? StateId { get; }
    Guid? GuardianId { get; }

    bool IsStateAdmin { get; }
    bool IsRegionalAdmin { get; }
    bool IsDirectorate { get; }
    bool IsTeacher { get; }
    bool IsStudent { get; }
    bool IsGuardian { get; }
}
