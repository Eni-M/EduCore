namespace EduCore.Dtos;

// Read-only by nature -- audit entries are written internally by services as a
// side effect of other actions, never submitted directly through the API.
public record AuditLogListDto(Guid Id, Guid? UserId, string? UserName, string Action, DateTime Timestamp);
