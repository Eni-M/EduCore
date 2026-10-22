using System.Diagnostics;
using System.Security.Claims;
using EduCore.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EduCore.Data;

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

    // Entities whose old and new values are safe and useful to keep: a mark is a legal record,
    // and "4 -> 2" is what a grade dispute needs. Every other entity records which fields
    // changed, never their values -- several hold encrypted or personal data.
    private static readonly HashSet<Type> ValueAudited = [typeof(Mark), typeof(GradingPeriod)];

    private void AddAuditLogs(DbContext? context)
    {
        if (context is null)
            return;

        var entries = context.ChangeTracker.Entries()
            // Statistics snapshots are derived data that the app rewrites on its own schedule,
            // not something a user did. Auditing them would bury real activity under refresh
            // noise in the table that is already the largest in the database.
            .Where(e => e.Entity is not AuditLog and not SchoolStatisticsSnapshot
                        && e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        if (entries.Count == 0)
            return;

        var userId = CurrentUserId();
        var timestamp = DateTime.UtcNow;

        // The W3C trace id, the same one GlobalExceptionHandler reports, so an audit row lines up
        // with the request's log lines and spans. TraceIdentifier is a per-process counter.
        var traceId = Activity.Current?.TraceId.ToString() ?? httpContextAccessor.HttpContext?.TraceIdentifier;

        context.AddRange(entries.Select(e => new AuditLog
        {
            UserId = userId,
            SchoolId = (e.Entity as ISchoolScoped)?.SchoolId,
            Action = $"{e.State} {e.Entity.GetType().Name} ({Key(e)})",
            Changes = e.State == EntityState.Modified ? Changes(e) : null,
            Timestamp = timestamp,
            TraceId = traceId
        }));
    }

    private static string? Changes(EntityEntry entry)
    {
        var withValues = ValueAudited.Contains(entry.Entity.GetType());

        var changed = entry.Properties
            .Where(p => p.IsModified && !p.Metadata.IsShadowProperty()
                        && !Equals(p.OriginalValue, p.CurrentValue))
            .Select(p => withValues
                ? $"{p.Metadata.Name}: {p.OriginalValue ?? "null"} -> {p.CurrentValue ?? "null"}"
                : p.Metadata.Name)
            .ToList();

        return changed.Count == 0 ? null : string.Join("; ", changed);
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
