using System.Security.Claims;
using EduCore.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EduCore.Data;


public class AuditSaveChangesInterceptor(IHttpContextAccessor httpContextAccessor) : SaveChangesInterceptor
{
    private readonly List<PendingAudit> _pending = [];

    // Set while this interceptor is saving its own AuditLog rows, so that save doesn't
    // recurse back in and audit the audit trail.
    private bool _writingAuditLog;

    // Captured with the entries rather than at write time: by the time the rows are written
    // the request may already have moved on, and the trail should name whoever made the change.
    private Guid? _userId;

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        CollectPendingEntries(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        CollectPendingEntries(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        var logs = BuildAuditLogs();
        if (logs.Count > 0 && eventData.Context is { } context)
        {
            _writingAuditLog = true;
            try
            {
                context.AddRange(logs);
                context.SaveChanges();
            }
            finally
            {
                _writingAuditLog = false;
            }
        }

        return base.SavedChanges(eventData, result);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        var logs = BuildAuditLogs();
        if (logs.Count > 0 && eventData.Context is { } context)
        {
            _writingAuditLog = true;
            try
            {
                context.AddRange(logs);
                await context.SaveChangesAsync(cancellationToken);
            }
            finally
            {
                _writingAuditLog = false;
            }
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    // A failed save changed nothing, so there is nothing to log -- drop what we collected
    // instead of leaking it into whatever the caller tries next on the same context.
    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        _pending.Clear();
        base.SaveChangesFailed(eventData);
    }

    public override Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        _pending.Clear();
        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    private void CollectPendingEntries(DbContext? context)
    {
        if (context is null || _writingAuditLog)
            return;

        _pending.Clear();

        var entries = context.ChangeTracker.Entries()
            .Where(e => e.Entity is not AuditLog
                        && e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted);

        foreach (var entry in entries)
        {

            if (entry.State is EntityState.Modified && !entry.Properties.Any(p => p.IsModified))
                continue;
            
            var key = entry.State is EntityState.Added ? null : DescribeKey(entry);

            _pending.Add(new PendingAudit(entry, entry.State, entry.Entity.GetType().Name, key));
        }

        if (_pending.Count > 0)
            _userId = GetCurrentUserId();
    }

    private List<AuditLog> BuildAuditLogs()
    {
        if (_writingAuditLog || _pending.Count == 0)
            return [];

        var timestamp = DateTime.UtcNow;

        var logs = _pending
            .Select(p => new AuditLog
            {
                UserId = _userId,
                Action = $"{p.State} {p.EntityName} ({p.Key ?? DescribeKey(p.Entry)})",
                Timestamp = timestamp
            })
            .ToList();

        _pending.Clear();
        return logs;
    }

    private static string DescribeKey(EntityEntry entry) =>
        string.Join(",", entry.Properties
            .Where(p => p.Metadata.IsPrimaryKey())
            .Select(p => p.CurrentValue?.ToString() ?? "null"));

    private Guid? GetCurrentUserId()
    {
        var principal = httpContextAccessor.HttpContext?.User;
        var idClaim = principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? principal?.FindFirst("sub")?.Value;

        return Guid.TryParse(idClaim, out var id) ? id : null;
    }

    private sealed record PendingAudit(EntityEntry Entry, EntityState State, string EntityName, string? Key);
}
