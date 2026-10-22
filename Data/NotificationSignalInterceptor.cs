using EduCore.Models;
using EduCore.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EduCore.Data;

// Wakes the bell streams after any save that wrote notification rows -- a new notification, or a
// user marking one read. Hanging the signal off the write means a notification staged by any
// service, in the same save as the change it reports, signals without that service knowing the
// streams exist; and a save that rolls back signals nothing.
public class NotificationSignalInterceptor(NotificationSignals signals) : SaveChangesInterceptor
{
    private readonly Dictionary<DbContext, List<(Guid SchoolId, Enums.UserRole Role)>> _pending = new();

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Collect(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
    {
        Collect(eventData.Context);
        return base.SavingChangesAsync(eventData, result, ct);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        Signal(eventData.Context);
        return base.SavedChanges(eventData, result);
    }

    public override ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result, CancellationToken ct = default)
    {
        Signal(eventData.Context);
        return base.SavedChangesAsync(eventData, result, ct);
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData) => Forget(eventData.Context);

    public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken ct = default)
    {
        Forget(eventData.Context);
        return Task.CompletedTask;
    }

    private void Collect(DbContext? context)
    {
        if (context is null)
            return;

        var changed = context.ChangeTracker.Entries<Notification>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified)
            .Select(e => (e.Entity.SchoolId, e.Entity.RecipientRole))
            .ToList();

        // A read row changes only its reader's count, but the reader's stream is keyed by role and
        // school like everyone else's; waking the group costs one recount each.
        changed.AddRange(context.ChangeTracker.Entries<NotificationRead>()
            .Where(e => e.State == EntityState.Added)
            .Select(e => (e.Entity.SchoolId, e.Entity.RecipientRole)));

        if (changed.Count > 0)
            _pending[context] = changed;
    }

    private void Signal(DbContext? context)
    {
        if (context is null || !_pending.Remove(context, out var changed))
            return;

        foreach (var (schoolId, role) in changed.Distinct())
            signals.Changed(schoolId, role);
    }

    private void Forget(DbContext? context)
    {
        if (context is not null)
            _pending.Remove(context);
    }
}
