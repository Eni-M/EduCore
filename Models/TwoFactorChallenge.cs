using EduCore.Enums;

namespace EduCore.Models;

// What a second step is being asked for. Kept on the challenge so a code emailed to finish a
// sign-in cannot be replayed against the password-change endpoint, or the other way round.
public enum TwoFactorPurpose
{
    SignIn = 0,
    PasswordChange = 1
}

// An outstanding second step: a short code emailed to the account's address, which the caller
// must present along with the challenge's id to finish what they started.
//
// Only the SHA-256 hash of the code is stored, for the same reason PasswordResetToken stores a
// hash: the plaintext exists in the inbox and nowhere else, so a database leak cannot be
// replayed. Not school-scoped -- it is resolved before the caller has an identity to scope by.
//
// Six digits is a million possibilities, which is only safe because the attempt count is capped
// and the window is minutes. Both of those are load-bearing, not tidiness.
public class TwoFactorChallenge
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    // SHA-256 of the plaintext code, base64. Never the code itself.
    public string CodeHash { get; set; } = null!;

    public TwoFactorPurpose Purpose { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }

    // Set the moment the challenge is answered, so a replay is rejected rather than accepted.
    public DateTime? CompletedAt { get; set; }

    // Wrong codes so far. A challenge that runs out of attempts is dead even before it expires,
    // which is what keeps a million-guess space out of reach.
    public int FailedAttempts { get; set; }

    public User User { get; set; } = null!;

    public bool IsOpen(DateTime nowUtc, int maxAttempts) =>
        CompletedAt is null && nowUtc < ExpiresAt && FailedAttempts < maxAttempts;
}
