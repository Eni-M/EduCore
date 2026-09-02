using System.Linq;
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

    public IReadOnlyCollection<Guid> SchoolIds =>
        Principal?.FindAll(ClaimNames.SchoolId)
            .Select(c => Guid.TryParse(c.Value, out var value) ? value : (Guid?)null)
            .Where(v => v is not null)
            .Select(v => v!.Value)
            .Distinct()
            .ToArray()
        ?? [];

    public Guid? SchoolId => SchoolIds.Count == 1 ? SchoolIds.First() : null;

    public Guid? TeacherId => ReadGuid(ClaimNames.TeacherId);
    public Guid? StudentId => ReadGuid(ClaimNames.StudentId);
    public Guid? CityId => ReadGuid(ClaimNames.CityId);
    public Guid? StateId => ReadGuid(ClaimNames.StateId);
    public Guid? GuardianId => ReadGuid(ClaimNames.GuardianId);

    public bool IsStateAdmin => Role == UserRole.StateAdmin;
    public bool IsRegionalAdmin => Role == UserRole.RegionalAdmin;
    public bool IsDirectorate => Role == UserRole.Directorate;
    public bool IsTeacher => Role == UserRole.Teacher;
    public bool IsStudent => Role == UserRole.Student;
    public bool IsGuardian => Role == UserRole.Guardian;

    private Guid? ReadGuid(string claimType) =>
        Guid.TryParse(Principal?.FindFirst(claimType)?.Value, out var value) ? value : null;
}
