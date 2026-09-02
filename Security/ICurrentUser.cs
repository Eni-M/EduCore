using EduCore.Enums;

namespace EduCore.Security;

public interface ICurrentUser
{
    Guid UserId { get; }
    UserRole? Role { get; }

    /// <summary>The single school this user is pinned to, or null when they span zero or several.</summary>
    Guid? SchoolId { get; }

    /// <summary>Every school this user may act within — one entry for most roles, several for a
    /// guardian with children in more than one school.</summary>
    IReadOnlyCollection<Guid> SchoolIds { get; }

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
