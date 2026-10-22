using System.Diagnostics;
using System.Security.Claims;
using EduCore.Data;
using EduCore.Models;

namespace EduCore.Services;

// Audit rows for what the save interceptor cannot see: reads of sensitive data (medical records,
// national identifiers) and authentication events. Law 06/L-082 gives 72 hours to report a
// breach, which needs a record of who looked at what and of sign-in failures to detect one.
//
// Saves immediately, so call it only where nothing else is pending on the context: after a read,
// or in a sign-in path that has already saved.
public interface IAuditTrail
{
    Task RecordAsync(string action, CancellationToken ct, Guid? userId = null, Guid? schoolId = null);
}

public class AuditTrail(AppDbContext db, IHttpContextAccessor httpContextAccessor) : IAuditTrail
{
    public async Task RecordAsync(string action, CancellationToken ct, Guid? userId = null, Guid? schoolId = null)
    {
        var http = httpContextAccessor.HttpContext;

        db.AuditLogs.Add(new AuditLog
        {
            UserId = userId ?? (Guid.TryParse(http?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null),
            SchoolId = schoolId,
            Action = action,
            Timestamp = DateTime.UtcNow,
            TraceId = Activity.Current?.TraceId.ToString() ?? http?.TraceIdentifier,
        });

        await db.SaveChangesAsync(ct);
    }
}
