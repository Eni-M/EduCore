using EduCore.Exceptions;
using Microsoft.Extensions.Caching.Memory;

namespace EduCore.Security;

// Wrong authenticator codes are counted per user, not per challenge. A challenge's own attempt cap
// resets with every new sign-in, and anyone holding the password can start as many as they like;
// with a three-code window that would make the million-code space guessable. Five wrong codes in
// fifteen minutes stop that user's app codes for the rest of the window, whichever challenge or
// setup step they came through.
// ponytail: in-process counter, one instance; move it to the database or a shared cache before
// running more than one replica (see the scale-out checklist in docs/ARCHITECTURE.md).
public sealed class AuthenticatorThrottle(IMemoryCache cache)
{
    private const int MaxFailures = 5;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(15);

    private sealed record Failures(int Count, DateTimeOffset ExpiresAt);

    private static string Key(Guid userId) => $"authenticator-failures:{userId}";

    public void EnsureAllowed(Guid userId)
    {
        if (cache.Get<Failures>(Key(userId))?.Count >= MaxFailures)
            throw new TooManyRequestsException("Too many wrong authenticator codes. Wait 15 minutes and try again.");
    }

    // The window runs from the first failure; later ones do not extend it.
    public void Failed(Guid userId)
    {
        var current = cache.Get<Failures>(Key(userId));
        var expiresAt = current?.ExpiresAt ?? DateTimeOffset.UtcNow.Add(Window);
        cache.Set(Key(userId), new Failures((current?.Count ?? 0) + 1, expiresAt), expiresAt);
    }

    public void Succeeded(Guid userId) => cache.Remove(Key(userId));
}
