namespace EduCore.Models;

public class AuditLog
{
    public Guid Id { get; set; }

    // No FK to User by design -- an audit trail can never be allowed to fail the write it's
    // auditing just because the acting user's row is gone (revoked account, stale token, etc).
    public Guid? UserId { get; set; }
    public string Action { get; set; } = null!;
    public DateTime Timestamp { get; set; }
}
