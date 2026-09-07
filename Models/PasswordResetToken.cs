namespace EduCore.Models;

// A single-use, time-limited grant to set a new password without knowing the old one.
//
// Only the SHA-256 hash of the token is stored: the plaintext exists in the reset email and
// nowhere else, so a database leak cannot be replayed into account takeover. Not school-scoped
// — the account it belongs to may span several schools, and the token is resolved before the
// caller has any identity to scope by.
public class PasswordResetToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    // SHA-256 of the plaintext token, base64. Never the token itself.
    public string TokenHash { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }

    // Set the moment the token is redeemed, so a replay is rejected rather than accepted twice.
    public DateTime? UsedAt { get; set; }

    public User User { get; set; } = null!;

    public bool IsRedeemable(DateTime nowUtc) => UsedAt is null && nowUtc < ExpiresAt;
}
