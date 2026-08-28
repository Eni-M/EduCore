namespace EduCore.Models;

public class AuditLog
{
    public Guid Id { get; set; }

    // No FK to User by design -- an audit trail can never be allowed to fail the write it's
    // auditing just because the acting user's row is gone (revoked account, stale token, etc).
    public Guid? UserId { get; set; }
    public string Action { get; set; } = null!;
    public DateTime Timestamp { get; set; }

    // The request that caused this change, as reported by HttpContext.TraceIdentifier -- the
    // same value GlobalExceptionHandler returns to the caller and writes to the log. It is
    // what ties the three together: one save writes one row per changed entity, and sharing a
    // trace id is what shows they were one action rather than several. Null for writes with no
    // request behind them (start-up seeding, background work).
    public string? TraceId { get; set; }
}
