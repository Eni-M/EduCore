using EduCore.Enums;

namespace EduCore.Security;

public interface ICurrentUser
{
    Guid UserId { get; }
    UserRole? Role { get; }

    Guid? SchoolId { get; }
    Guid? TeacherId { get; }
    Guid? StudentId { get; }

    bool IsSuperAdmin { get; }
    bool IsAdmin { get; }
    bool IsTeacher { get; }
    bool IsStudent { get; }
}
