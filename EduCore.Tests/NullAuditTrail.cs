using EduCore.Services;

namespace EduCore.Tests;

// For unit tests of services that record audit rows: the rows are the save interceptor's and
// AuditTrail's concern, covered where they are tested, not in every service's tests.
public sealed class NullAuditTrail : IAuditTrail
{
    public List<string> Recorded { get; } = [];

    public Task RecordAsync(string action, CancellationToken ct, Guid? userId = null, Guid? schoolId = null)
    {
        Recorded.Add(action);
        return Task.CompletedTask;
    }
}
