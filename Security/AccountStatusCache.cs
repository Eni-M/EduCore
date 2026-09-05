using EduCore.Repositories.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace EduCore.Security;

/// <summary>
/// The two account facts every authenticated request has to re-check, and the only two: a JWT is
/// valid for an hour, so neither deactivation nor a forced password rotation can be carried in
/// the token -- both change during its lifetime.
/// </summary>
public record AccountStatus(bool Exists, bool IsDeactivated, bool MustChangePassword)
{
    public static readonly AccountStatus Missing = new(false, false, false);
}

public interface IAccountStatusCache
{
    Task<AccountStatus> GetAsync(Guid userId, CancellationToken ct);
}

/// <summary>
/// Caches the per-request revocation lookup.
///
/// Before this, every single request to <c>/api</c> ran one SELECT against Users purely to
/// answer "is this account still allowed in" -- the highest-frequency query in the application
/// by a wide margin, and the same answer nearly every time.
///
/// Revocation stays immediate rather than eventually-consistent: <see cref="AccountStatusInvalidationInterceptor"/>
/// drops a user's entry as part of the write that deactivates them or rotates their password, so
/// the very next request re-reads. The TTL below is a backstop for the case that eviction cannot
/// cover -- another process writing to the same database -- and not the mechanism.
///
/// What is cached is the account's status, never an authorization decision: the filters still
/// decide what to do with it, and every scope claim still comes from the token.
/// </summary>
public class AccountStatusCache(IMemoryCache cache, IUserRepository users) : IAccountStatusCache
{
    // Memory is bounded by this window, not by user count: only accounts that made a request in
    // the last minute hold an entry, and each is three booleans.
    //
    // Short enough that a second instance writing to this database cannot keep a deactivated
    // account alive for long, long enough to collapse a burst of requests from one user into a
    // single read. With one instance, in-process eviction means this is rarely reached at all.
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(60);

    // Public so the invalidation interceptor and its tests name the same entry.
    public static string Key(Guid userId) => $"account-status:{userId}";

    public async Task<AccountStatus> GetAsync(Guid userId, CancellationToken ct)
    {
        if (cache.TryGetValue(Key(userId), out AccountStatus? cached) && cached is not null)
            return cached;

        var user = await users.GetByIdAsync(userId, ct);

        var status = user is null
            ? AccountStatus.Missing
            : new AccountStatus(true, user.DeactivatedAt is not null, user.MustChangePassword);

        // A deleted or unknown user is cached too. Without that, a stale token for an account
        // that no longer exists would read the database on every request it makes -- exactly the
        // load this cache exists to remove, and trivially reproducible by anyone holding one.
        cache.Set(Key(userId), status, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = Ttl
        });

        return status;
    }
}
