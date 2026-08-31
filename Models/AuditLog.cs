namespace EduCore.Models;

public class AuditLog
{
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }
    public string Action { get; set; } = null!;
    public DateTime Timestamp { get; set; }

    public string? TraceId { get; set; }
}
