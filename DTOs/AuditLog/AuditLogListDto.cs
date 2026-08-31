namespace EduCore.Dtos.AuditLog;

public record AuditLogListDto(
    Guid Id,
    Guid? UserId,
    string? UserName,
    string Action,
    DateTime Timestamp,
    string? TraceId);
