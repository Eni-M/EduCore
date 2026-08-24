namespace EduCore.Dtos.AuditLog;

// Read-only by nature -- audit entries are written internally by services/the interceptor as
// a side effect of other actions, never submitted directly through the API.
public record AuditLogListDto(Guid Id, Guid? UserId, string? UserName, string Action, DateTime Timestamp);
