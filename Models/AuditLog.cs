namespace EduCore.Models;

public class AuditLog
{
    public Guid Id { get; private set; }
    public Guid? UserId { get; private set; }
    public string Action { get; private set; }
    public DateTime Timestamp { get; private set; }

    public AuditLog(Guid? userId, string action)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);

        Id = Guid.NewGuid();
        UserId = userId;
        Action = action;
        Timestamp = DateTime.UtcNow;
    }
}