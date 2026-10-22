namespace EduCore.Models;

public class AuditLog
{
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }

    // The school the change or read concerns, when it concerns one -- lets an office see its own
    // trail without reading every other school's.
    public Guid? SchoolId { get; set; }

    public string Action { get; set; } = null!;
    public DateTime Timestamp { get; set; }

    public string? TraceId { get; set; }

    // Which fields changed, and for marks the old and new values -- "Value: 4 -> 2". Field names
    // only for encrypted entities: copying their plaintext here would undo the encryption.
    public string? Changes { get; set; }
}
