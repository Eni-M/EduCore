using EduCore.Exceptions;
using Microsoft.Extensions.Caching.Memory;

namespace EduCore.Security;

// What a throttled attempt is checking. Counted apart, so wrong passwords do not use up codes or
// the other way round.
public enum AttemptKind
{
    Code,
    Password
}

// Wrong two-step codes are counted per user, not per challenge. A challenge's own attempt cap
// resets with every new sign-in, and anyone holding the password can start as many as they like;
// that would make the million-code space guessable, emailed or from an app. Five attempts in
// fifteen minutes stop that user's codes for the rest of the window, whichever challenge or setup
// step they came through.
//
// The current password asked for again on the account screens is counted the same way. A
// borrowed session must not get to guess, at the speed of the general rate limit, the password
// that stands between it and removing the owner's authenticator.
//
// An attempt is reserved -- counted -- before it is checked, under a lock, and only a correct one
// gives it back. Checking first and counting after would let a burst of concurrent requests all
// pass the check before any of them was counted.
// ponytail: in-process counter, one instance; move it to the database (an atomic
// UPDATE ... WHERE count < 5) before running more than one replica.
public sealed class AttemptThrottle(IMemoryCache cache)
{
    private const int MaxAttempts = 5;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(15);
    private static readonly object Gate = new();

    private sealed record Attempts(int Count, DateTimeOffset ExpiresAt);

    private static string Key(AttemptKind kind, Guid userId) => $"attempts:{kind}:{userId}";

    // Counts this attempt, or refuses it if the window's attempts are used up.
    public void Reserve(AttemptKind kind, Guid userId)
    {
        lock (Gate)
        {
            var current = cache.Get<Attempts>(Key(kind, userId));
            if (current?.Count >= MaxAttempts)
                throw new TooManyRequestsException(kind == AttemptKind.Code
                    ? "Too many wrong codes. Wait 15 minutes and try again."
                    : "Too many wrong passwords. Wait 15 minutes and try again.");

            // The window runs from the first attempt; later ones do not extend it.
            var expiresAt = current?.ExpiresAt ?? DateTimeOffset.UtcNow.Add(Window);
            cache.Set(Key(kind, userId), new Attempts((current?.Count ?? 0) + 1, expiresAt), expiresAt);
        }
    }

    // A correct code or password clears the count.
    public void Succeeded(AttemptKind kind, Guid userId)
    {
        lock (Gate)
            cache.Remove(Key(kind, userId));
    }
}
