using EduCore.Exceptions;
using Microsoft.Extensions.Caching.Memory;

namespace EduCore.Security;

// Wrong authenticator codes are counted per user, not per challenge. A challenge's own attempt cap
// resets with every new sign-in, and anyone holding the password can start as many as they like;
// with a three-code window that would make the million-code space guessable. Five attempts in
// fifteen minutes stop that user's app codes for the rest of the window, whichever challenge or
// setup step they came through.
//
// An attempt is reserved -- counted -- before the code is checked, under a lock, and only a
// correct code gives it back. Checking first and counting after would let a burst of concurrent
// requests all pass the check before any of them was counted.
// ponytail: in-process counter, one instance; move it to the database (an atomic
// UPDATE ... WHERE count < 5) before running more than one replica.
public sealed class AuthenticatorThrottle(IMemoryCache cache)
{
    private const int MaxAttempts = 5;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(15);
    private static readonly object Gate = new();

    private sealed record Attempts(int Count, DateTimeOffset ExpiresAt);

    private static string Key(Guid userId) => $"authenticator-attempts:{userId}";

    // Counts this attempt, or refuses it if the window's attempts are used up.
    public void Reserve(Guid userId)
    {
        lock (Gate)
        {
            var current = cache.Get<Attempts>(Key(userId));
            if (current?.Count >= MaxAttempts)
                throw new TooManyRequestsException("Too many wrong authenticator codes. Wait 15 minutes and try again.");

            // The window runs from the first attempt; later ones do not extend it.
            var expiresAt = current?.ExpiresAt ?? DateTimeOffset.UtcNow.Add(Window);
            cache.Set(Key(userId), new Attempts((current?.Count ?? 0) + 1, expiresAt), expiresAt);
        }
    }

    // A correct code clears the count.
    public void Succeeded(Guid userId)
    {
        lock (Gate)
            cache.Remove(Key(userId));
    }
}
