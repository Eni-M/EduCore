using System.Security.Cryptography;
using System.Text;
using EduCore.Enums;
using EduCore.Exceptions;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using EduCore.Services.Email;
using Microsoft.Extensions.Options;

namespace EduCore.Services.Account;

public interface ITwoFactorService
{
    // Whether finishing a sign-in for this user needs a second step: always for the roles that
    // manage accounts, and for anybody who has switched it on.
    bool SignInRequiresSecondStep(User user);

    // Issues a challenge and emails the code. Returns the challenge id, which is not a secret --
    // the code is. Both are needed to complete it.
    Task<Guid> IssueAsync(User user, TwoFactorPurpose purpose, CancellationToken ct);

    // Consumes a challenge. Throws if it is unknown, expired, out of attempts, already used,
    // for a different purpose, or the code is wrong. Returns the user it belonged to.
    Task<User> CompleteAsync(Guid challengeId, string code, TwoFactorPurpose purpose, CancellationToken ct);
}

// The second step: a six-digit code emailed to the address on the account.
//
// Weaker than an authenticator app, and chosen deliberately -- it needs no smartphone, which
// matters for a system whose users include every parent in a school network, and it reuses the
// mail path that is already there. What it protects against is a stolen or guessed password,
// which is the realistic attack here; it does not protect against a compromised mailbox, and it
// is not pretending to.
public class TwoFactorService(
    ITwoFactorChallengeRepository challenges,
    IEmailSender email,
    IOptions<TwoFactorOptions> options,
    IOptions<PasswordResetOptions> passwordReset) : ITwoFactorService
{
    private readonly TwoFactorOptions _options = options.Value;
    private readonly string _appUrl = AccountMailTemplates.AppUrlFrom(passwordReset.Value.ResetUrlTemplate);

    public bool SignInRequiresSecondStep(User user) =>
        _options.IsRequiredFor(user.Role) || user.TwoFactorEnabled;

    public async Task<Guid> IssueAsync(User user, TwoFactorPurpose purpose, CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        // One live challenge per purpose: asking for a new code retires the previous one, so a
        // code still sitting in an inbox cannot be used later.
        await challenges.InvalidateOutstandingAsync(user.Id, purpose, now, ct);

        var code = GenerateCode();

        var challenge = new TwoFactorChallenge
        {
            UserId = user.Id,
            CodeHash = Hash(code),
            Purpose = purpose,
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(_options.CodeLifetimeMinutes)
        };

        await challenges.AddAsync(challenge);
        await challenges.SaveChangesAsync(ct);

        var (subject, body) = Compose(user, code, purpose);
        await email.SendAsync(user.Email, subject, body, ct);

        return challenge.Id;
    }

    public async Task<User> CompleteAsync(
        Guid challengeId, string code, TwoFactorPurpose purpose, CancellationToken ct)
    {
        var challenge = await challenges.GetOpenAsync(challengeId, ct);

        // One message for every failure below. Telling the caller which of "no such challenge",
        // "expired", "wrong purpose" and "wrong code" applied would let them map the state of
        // somebody else's sign-in.
        if (challenge is null
            || challenge.Purpose != purpose
            || !challenge.IsOpen(DateTime.UtcNow, _options.MaxAttempts))
            throw new UnauthorizedException("That code is not valid. Ask for a new one.");

        if (!Matches(challenge.CodeHash, code))
        {
            // Counted before the throw and saved, or the cap would never be reached.
            challenge.FailedAttempts++;
            challenges.Update(challenge);
            await challenges.SaveChangesAsync(ct);

            throw new UnauthorizedException("That code is not valid. Ask for a new one.");
        }

        challenge.CompletedAt = DateTime.UtcNow;
        challenges.Update(challenge);
        await challenges.SaveChangesAsync(ct);

        return challenge.User;
    }

    // RandomNumberGenerator, not Random: this is a credential, and a predictable one is no
    // second step at all. GetInt32 is uniform over the range, so no digit is favoured.
    private static string GenerateCode() =>
        RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

    private static string Hash(string code) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(code)));

    // Fixed-time comparison of the hashes. Comparing strings with == would return as soon as a
    // character differed, which leaks how much of a guess was right.
    private static bool Matches(string storedHash, string presentedCode) =>
        CryptographicOperations.FixedTimeEquals(
            Convert.FromBase64String(storedHash),
            SHA256.HashData(Encoding.UTF8.GetBytes(presentedCode ?? string.Empty)));

    private (string Subject, EmailBody Body) Compose(User user, string code, TwoFactorPurpose purpose)
    {
        var minutes = _options.CodeLifetimeMinutes;

        return purpose == TwoFactorPurpose.SignIn
            ? ("Your EduCore sign-in code",
                EmailLayout.Render(
                    "Your sign-in code",
                    [
                        new EmailBlock.Paragraph($"Hello {user.FirstName},"),
                        new EmailBlock.Paragraph(
                            $"Enter this code to finish signing in. It expires in {minutes} minutes and works once."),
                        new EmailBlock.Code(code),
                        new EmailBlock.Note(
                            "If you were not signing in, someone may know your password. Change it and tell your school office."),
                    ],
                    _appUrl))
            : ("Your EduCore code for changing your password",
                EmailLayout.Render(
                    "Confirm your password change",
                    [
                        new EmailBlock.Paragraph($"Hello {user.FirstName},"),
                        new EmailBlock.Paragraph(
                            $"Enter this code to confirm the change. It expires in {minutes} minutes and works once."),
                        new EmailBlock.Code(code),
                        new EmailBlock.Note(
                            "If you did not ask to change your password, ignore this email -- nothing has changed -- and tell your school office."),
                    ],
                    _appUrl));
    }
}
