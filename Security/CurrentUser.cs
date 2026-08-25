using System.Security.Claims;
using EduCore.Enums;
using EduCore.Exceptions;

namespace EduCore.Security;

public class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public Guid UserId =>
        ReadGuid(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedException("The request is not authenticated.");

    public UserRole? Role =>
        Enum.TryParse<UserRole>(Principal?.FindFirst(ClaimTypes.Role)?.Value, out var role) ? role : null;

    public Guid? SchoolId => ReadGuid(ClaimNames.SchoolId);
    public Guid? TeacherId => ReadGuid(ClaimNames.TeacherId);
    public Guid? StudentId => ReadGuid(ClaimNames.StudentId);

    public bool IsSuperAdmin => Role == UserRole.SuperAdmin;
    public bool IsAdmin => Role == UserRole.Admin;
    public bool IsTeacher => Role == UserRole.Teacher;
    public bool IsStudent => Role == UserRole.Student;

    private Guid? ReadGuid(string claimType) =>
        Guid.TryParse(Principal?.FindFirst(claimType)?.Value, out var value) ? value : null;
}
