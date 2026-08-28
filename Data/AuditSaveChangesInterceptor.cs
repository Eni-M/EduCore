using System.Security.Claims;
using EduCore.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EduCore.Data;

// Adds one AuditLog row per changed entity to the same save, so the trail and the change it
// describes commit or fail together. This works because EF assigns Guid keys when an entity
// is added, not when it is saved -- so the ids are already known here.
public class AuditSaveChangesInterceptor(IHttpContextAccessor httpContextAccessor) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        AddAuditLogs(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        AddAuditLogs(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void AddAuditLogs(DbContext? context)
    {
        if (context is null)
            return;

        var changes = context.ChangeTracker.Entries()
            .Where(e => e.Entity is not AuditLog
                        && e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Select(e => $"{e.State} {e.Entity.GetType().Name} ({Key(e)})")
            .ToList();

        if (changes.Count == 0)
            return;

        var userId = CurrentUserId();
        var timestamp = DateTime.UtcNow;

        // Read once, not per row: every entry this save produces belongs to the same request,
        // and sharing one trace id is exactly what makes them readable as a single action.
        var traceId = httpContextAccessor.HttpContext?.TraceIdentifier;

        context.AddRange(changes.Select(action => new AuditLog
        {
            UserId = userId,
            Action = action,
            Timestamp = timestamp,
            TraceId = traceId
        }));
    }

    private static string Key(EntityEntry entry) =>
        string.Join(",", entry.Properties
            .Where(p => p.Metadata.IsPrimaryKey())
            .Select(p => p.CurrentValue?.ToString() ?? "null"));

    private Guid? CurrentUserId()
    {
        var id = httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(id, out var userId) ? userId : null;
    }
}
