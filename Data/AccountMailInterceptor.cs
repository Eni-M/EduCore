using EduCore.Models;
using EduCore.Services.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using EduCore.Services.Interfaces;

namespace EduCore.Data;

// Queues account emails from the write that causes them.
//
// An interceptor rather than a call in each service, for the same reason
// AccountStatusInvalidationInterceptor is one: accounts are created in seven
// different services today, and a welcome email wired at each of them is a welcome email the
// eighth one forgets. Hanging it off the save covers code nobody has written yet.
//
// Only queued after the write commits. A message about a password that was never saved is worse
// than no message -- it tells someone their password changed when it did not, and invites them
// to report a breach that did not happen.
public class AccountMailInterceptor(IAccountMailQueue queue) : SaveChangesInterceptor
{
    // Collected while the change tracker still reports what changed; entries are Unchanged by the
    // time SavedChanges runs, and the original password hash is gone with them.
    private readonly Dictionary<DbContext, List<AccountMailRequest>> _pending = new();

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
        Flush(eventData.Context);
        return base.SavedChanges(eventData, result);
    }

    public override ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result, CancellationToken ct = default)
    {
        Flush(eventData.Context);
        return base.SavedChangesAsync(eventData, result, ct);
    }

    // A failed save sends nothing. The collected requests are dropped rather than flushed.
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

        var requests = new List<AccountMailRequest>();

        foreach (var entry in context.ChangeTracker.Entries<User>())
        {
            var user = entry.Entity;

            // Deactivated accounts are skipped throughout: mail to someone who has been removed
            // from the platform is at best noise and at worst tells a former member of staff that
            // their account is still live.
            if (user.DeactivatedAt is not null || string.IsNullOrWhiteSpace(user.Email))
                continue;

            switch (entry.State)
            {
                case EntityState.Added:
                    requests.Add(new AccountMailRequest(
                        AccountMailKind.Welcome, user.Email, user.FirstName));
                    break;

                case EntityState.Modified:
                    if (PasswordWasReplaced(entry))
                        requests.Add(new AccountMailRequest(
                            AccountMailKind.PasswordChanged, user.Email, user.FirstName));

                    var confirmed = entry.Property(u => u.TotpConfirmedAt);
                    if (confirmed.OriginalValue is null && confirmed.CurrentValue is not null)
                        requests.Add(new AccountMailRequest(AccountMailKind.AuthenticatorAdded, user.Email, user.FirstName));
                    else if (confirmed.OriginalValue is not null && confirmed.CurrentValue is null)
                        requests.Add(new AccountMailRequest(AccountMailKind.AuthenticatorRemoved, user.Email, user.FirstName));

                    // Told at the old address: the new one is whoever just took the account.
                    var oldEmail = entry.Property(u => u.Email).OriginalValue;
                    if (!string.IsNullOrWhiteSpace(oldEmail)
                        && !string.Equals(oldEmail, user.Email, StringComparison.OrdinalIgnoreCase))
                        requests.Add(new AccountMailRequest(
                            AccountMailKind.EmailChanged, oldEmail, user.FirstName, NewEmail: user.Email));
                    break;
            }
        }

        if (requests.Count > 0)
            _pending[context] = requests;
    }

    // The hash is what actually changed; MustChangePassword and TwoFactorEnabled move for other
    // reasons, and comparing the plaintext is impossible by design.
    //
    // IsModified alone is not enough: DbSet.Update marks every property modified whether or not
    // its value moved, so any service that saves a user that way -- switching two-step sign-in
    // on, say -- would otherwise tell them their password had changed. Compare the values.
    private static bool PasswordWasReplaced(EntityEntry<User> entry)
    {
        var hash = entry.Property(nameof(User.PasswordHash));

        return hash.IsModified
               && !string.Equals(
                   hash.OriginalValue as string, hash.CurrentValue as string, StringComparison.Ordinal);
    }

    private void Flush(DbContext? context)
    {
        if (context is null || !_pending.Remove(context, out var requests))
            return;

        foreach (var request in requests)
            queue.Enqueue(request);
    }

    private void Forget(DbContext? context)
    {
        if (context is not null)
            _pending.Remove(context);
    }
}
