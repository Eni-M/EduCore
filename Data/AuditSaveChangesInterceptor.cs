using System.Security.Claims;
using EduCore.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EduCore.Data;

// Basic, structural audit trail: WHO changed WHICH entity and HOW (Added/Modified/Deleted),
// added automatically to every SaveChanges call. Deliberately does not diff individual
// property values or filter out "unimportant" entities -- that's where this kind of
// interceptor tends to spiral in complexity. Add specific, richer explicit log calls
// in services for business events that need more than this gives you.
public class AuditSaveChangesInterceptor(IHttpContextAccessor httpContextAccessor) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        AddAuditEntries(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        AddAuditEntries(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    private void AddAuditEntries(DbContext? context)
    {
        if (context is null)
            return;

        var entries = context.ChangeTracker.Entries()
            .Where(e => e.Entity is not AuditLog
                        && e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        if (entries.Count == 0)
            return;

        var userId = GetCurrentUserId();

        foreach (var entry in entries)
        {
            var keyValues = string.Join(",", entry.Properties
                .Where(p => p.Metadata.IsPrimaryKey())
                .Select(p => p.CurrentValue?.ToString() ?? "null"));
            var action = $"{entry.State} {entry.Entity.GetType().Name} ({keyValues})";
            context.Add(new AuditLog { UserId = userId, Action = action, Timestamp = DateTime.UtcNow });
        }
    }

    private Guid? GetCurrentUserId()
    {
        var idClaim = httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(idClaim, out var id) ? id : null;
    }
}
