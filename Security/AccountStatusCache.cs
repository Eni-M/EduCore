using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EduCore.Configuration;
using EduCore.Data;
using EduCore.Repositories.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace EduCore.Security;

// The account facts a token cannot carry, because they change during its hour of validity.
public record AccountStatus(
    bool Exists, bool IsDeactivated, bool MustChangePassword, DateTime? SessionsValidFrom = null)
{
    public static readonly AccountStatus Missing = new(false, false, false);

    // Whether this token still speaks for the account: the account is live, and the token was
    // issued no earlier than the last sign-out or password change. Compared in whole seconds,
    // the precision of the iat claim, so the token issued in the same request as the cutoff
    // (a password change re-signs the caller in) is admitted. A token without iat predates the
    // claim and counts as issued at the epoch.
    public bool Admits(ClaimsPrincipal token)
    {
        if (!Exists || IsDeactivated) return false;
        if (SessionsValidFrom is not { } cutoff) return true;

        long.TryParse(token.FindFirst(JwtRegisteredClaimNames.Iat)?.Value, out var issuedAt);
        return issuedAt >= new DateTimeOffset(DateTime.SpecifyKind(cutoff, DateTimeKind.Utc)).ToUnixTimeSeconds();
    }
}

public interface IAccountStatusCache
{
    Task<AccountStatus> GetAsync(Guid userId, CancellationToken ct);
}

// Removes the per-request SELECT against Users that every /api call used to make. Revocation
// stays immediate: AccountStatusInvalidationInterceptor evicts on the write that deactivates a
// user, and the TTL is only a backstop for another process writing the same database.
public class AccountStatusCache(
    IMemoryCache cache,
    IUserRepository users,
    IOptions<CachingOptions> caching) : IAccountStatusCache
{
    // Bounded by this window rather than by user count. Short enough that a second instance
    // cannot keep a deactivated account alive for long.
    private readonly TimeSpan _ttl = TimeSpan.FromSeconds(caching.Value.AccountStatusSeconds);

    // Public so the invalidation interceptor and its tests name the same entry.
    public static string Key(Guid userId) => $"account-status:{userId}";

    public async Task<AccountStatus> GetAsync(Guid userId, CancellationToken ct)
    {
        if (cache.TryGetValue(Key(userId), out AccountStatus? cached) && cached is not null)
            return cached;

        var user = await users.GetByIdAsync(userId, ct);

        var status = user is null
            ? AccountStatus.Missing
            : new AccountStatus(
                true, user.DeactivatedAt is not null, user.MustChangePassword, user.SessionsValidFrom);

        // Unknown users are cached too, or a stale token would read the database every request.
        cache.Set(Key(userId), status, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _ttl
        });

        return status;
    }
}
