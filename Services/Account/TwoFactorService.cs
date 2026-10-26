using System.Security.Cryptography;
using System.Text;
using EduCore.Enums;
using EduCore.Exceptions;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using EduCore.Services.Email;
using Microsoft.Extensions.Options;
using EduCore.Security;
using EduCore.Services.Interfaces;

namespace EduCore.Services.Account;

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
    IOptions<PasswordResetOptions> passwordReset,
    IAuditTrail audit,
    AttemptThrottle throttle,
    IPasskeyService passkeys) : ITwoFactorService
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

        // With an authenticator app set up, the code is on the phone: nothing is emailed, and
        // the challenge holds no code of its own.
        // So is a sign-in that a passkey can answer.
        var noCode = user.HasAuthenticator
                     || (purpose == TwoFactorPurpose.SignIn && await passkeys.HasPasskeysAsync(user.Id, ct));
        var code = noCode ? null : GenerateCode();

        var challenge = new TwoFactorChallenge
        {
            UserId = user.Id,
            CodeHash = code is null ? "" : Hash(code),
            ViaAuthenticator = user.HasAuthenticator,
            Purpose = purpose,
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(_options.CodeLifetimeMinutes)
        };

        await challenges.AddAsync(challenge);
        await challenges.SaveChangesAsync(ct);

        if (code is not null)
        {
            var (subject, body) = Compose(user, code, purpose);
            await email.SendAsync(user.Email, subject, body, ct);
        }

        return challenge.Id;
    }

    public async Task<User> CompleteAsync(
        Guid challengeId, string code, TwoFactorPurpose purpose, CancellationToken ct, Guid? ownerId = null)
    {
        var challenge = await challenges.GetOpenAsync(challengeId, ct);
        var now = DateTime.UtcNow;

        // One message for every failure below. Telling the caller which of "no such challenge",
        // "expired", "wrong purpose" and "wrong code" applied would let them map the state of
        // somebody else's sign-in. Ownership is checked before anything is charged, so one account
        // cannot spend another's attempts.
        if (challenge is null
            || challenge.Purpose != purpose
            || (ownerId is { } owner && challenge.UserId != owner)
            || !challenge.IsOpen(now, _options.MaxAttempts))
            throw new UnauthorizedException("That code is not valid. Ask for a new one.");

        // Codes are capped per user as well as per challenge: a fresh challenge per sign-in
        // would otherwise mean fresh attempts for anyone holding the password.
        throttle.Reserve(AttemptKind.Code, challenge.UserId);

        // The attempt is charged in the database before the code is compared. Reading the count,
        // comparing and writing it back would let concurrent guesses all read the same count and
        // none of them be charged.
        if (!await challenges.TryTakeAttemptAsync(challenge.Id, _options.MaxAttempts, now, ct))
            throw new UnauthorizedException("That code is not valid. Ask for a new one.");

        var matched = challenge.ViaAuthenticator
            ? MatchesAuthenticator(challenge.User, code)
            : Matches(challenge.CodeHash, code);

        if (!matched)
        {
            await audit.RecordAsync($"Two-step code rejected ({challenge.Purpose})", ct, userId: challenge.UserId);
            throw new UnauthorizedException("That code is not valid. Ask for a new one.");
        }

        throttle.Succeeded(AttemptKind.Code, challenge.UserId);

        // Claimed in one UPDATE as well, so of two requests with the right code only one signs in.
        if (!await challenges.TryCompleteAsync(challenge.Id, now, ct))
            throw new UnauthorizedException("That code is not valid. Ask for a new one.");

        // Saves the authenticator step the code used (MatchesAuthenticator).
        await challenges.SaveChangesAsync(ct);

        return challenge.User;
    }

    // RandomNumberGenerator, not Random: this is a credential, and a predictable one is no
    // second step at all. GetInt32 is uniform over the range, so no digit is favoured, and the
    // format keeps the leading zeros that make every code the same length.
    private string GenerateCode()
    {
        var digits = _options.CodeLength;
        var exclusiveMax = (int)Math.Pow(10, digits);

        return RandomNumberGenerator.GetInt32(0, exclusiveMax).ToString(new string('0', digits));
    }

    private static string Hash(string code) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(code)));

    // Fixed-time comparison of the hashes. Comparing strings with == would return as soon as a
    // character differed, which leaks how much of a guess was right.
    // Records the step the code belonged to, so the same code cannot be used twice.
    private static bool MatchesAuthenticator(User user, string code)
    {
        if (!user.HasAuthenticator)
            return false;

        var step = Security.Totp.Verify(
            Security.Totp.FromBase32(user.TotpSecret!), code.Trim(), DateTimeOffset.UtcNow, user.TotpLastStep);
        if (step is null)
            return false;

        user.TotpLastStep = step;
        return true;
    }

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
