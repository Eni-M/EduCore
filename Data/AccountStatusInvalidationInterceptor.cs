using EduCore.Models;
using EduCore.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Memory;

namespace EduCore.Data;

/// <summary>
/// Drops a user's cached account status whenever that user row is written.
///
/// Deliberately an interceptor rather than a call in each service. Deactivation and password
/// rotation happen in five different places today (<c>UserService.SetActiveAsync</c>,
/// <c>ChangePasswordAsync</c>, <c>ResetPasswordAsync</c>, <c>PasswordResetService.ResetAsync</c>
/// and <c>PlatformOwnerSeeder</c>), and the cost of missing one is a revoked account that keeps
/// working. Hanging eviction off the write itself means a sixth path added later is covered
/// without anyone remembering to do it.
///
/// Over-eviction is free -- it costs one database read -- so this evicts on any User write,
/// not only on writes that touched the two fields.
/// </summary>
public class AccountStatusInvalidationInterceptor(IMemoryCache cache) : SaveChangesInterceptor
{
    // Collected while the change tracker still reports what changed; the entries are Unchanged
    // by the time SavedChanges runs.
    private readonly Dictionary<DbContext, List<Guid>> _pending = new();

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
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
        Evict(eventData.Context);
        return base.SavedChanges(eventData, result);
    }

    public override ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result, CancellationToken ct = default)
    {
        Evict(eventData.Context);
        return base.SavedChangesAsync(eventData, result, ct);
    }

    // A failed save still clears the collected ids, so they cannot leak into the next save on
    // this context. Evicting them too would be harmless, but there is nothing to evict: a write
    // that did not commit did not change what is cached.
    public override void SaveChangesFailed(DbContextErrorEventData eventData) =>
        Forget(eventData.Context);

    public override Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData, CancellationToken ct = default)
    {
        Forget(eventData.Context);
        return Task.CompletedTask;
    }

    private void Collect(DbContext? context)
    {
        if (context is null)
            return;

        var ids = context.ChangeTracker.Entries<User>()
            .Where(e => e.State is EntityState.Modified or EntityState.Deleted)
            .Select(e => e.Entity.Id)
            .Distinct()
            .ToList();

        if (ids.Count > 0)
            _pending[context] = ids;
    }

    private void Evict(DbContext? context)
    {
        if (context is null || !_pending.Remove(context, out var ids))
            return;

        foreach (var id in ids)
            cache.Remove(AccountStatusCache.Key(id));
    }

    private void Forget(DbContext? context)
    {
        if (context is not null)
            _pending.Remove(context);
    }
}
