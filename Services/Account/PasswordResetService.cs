using System.Security.Cryptography;
using System.Text;
using EduCore.Dtos.User;
using EduCore.Exceptions;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using EduCore.Services.Email;
using EduCore.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace EduCore.Services.Account;

public class PasswordResetService(
    IUserRepository users,
    IPasswordResetTokenRepository tokens,
    IPasswordHasher<User> passwordHasher,
    IEmailSender email,
    IOptions<PasswordResetOptions> options,
    ILogger<PasswordResetService> logger) : IPasswordResetService
{
    private readonly PasswordResetOptions _options = options.Value;

    public async Task RequestAsync(ForgotPasswordDto dto, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var user = await users.GetByEmailAsync(dto.Email, ct);

        // Unknown address, or a deactivated account: do the same nothing, and say the same
        // nothing to the caller. Any difference here — a status code, an error, or even a
        // noticeably faster reply — turns this endpoint into an account-existence oracle.
        if (user is null || user.DeactivatedAt is not null)
        {
            logger.LogInformation(
                "Password reset requested for an address with no active account. No mail sent.");
            return;
        }

        // Only one live link per account: requesting a second one retires the first, so a
        // stale email in an inbox cannot still open the account later.
        await tokens.InvalidateOutstandingAsync(user.Id, now, ct);

        var plaintext = GenerateToken();

        await tokens.AddAsync(new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = Hash(plaintext),
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(_options.TokenLifetimeMinutes)
        });

        await tokens.SaveChangesAsync(ct);

        var link = $"{_options.ResetUrlTemplate}?token={Uri.EscapeDataString(plaintext)}";

        await email.SendAsync(
            user.Email,
            "Reset your EduCore password",
            $"""
             Hello {user.FirstName},

             Someone asked to reset the password for your EduCore account. Open the link below
             to choose a new one. It works once, and expires in {_options.TokenLifetimeMinutes} minutes.

             {link}

             If this wasn't you, ignore this email — your password has not changed.
             """,
            ct);
    }

    public async Task ResetAsync(ResetPasswordDto dto, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var token = await tokens.GetByHashAsync(Hash(dto.Token), ct);

        // One message for unknown, expired and already-used alike. Distinguishing them tells
        // an attacker holding a guessed token which half of the guess was right.
        if (token is null || !token.IsRedeemable(now))
            throw new BusinessException(
                "This password reset link is no longer valid. Request a new one.");

        if (token.User.DeactivatedAt is not null)
            throw new BusinessException(
                "This password reset link is no longer valid. Request a new one.");

        token.User.SetPassword(dto.NewPassword, passwordHasher);

        // The user just proved control of the mailbox and chose the password themselves, so
        // there is nothing left to force on next sign-in.
        token.User.MustChangePassword = false;

        token.UsedAt = now;
        tokens.Update(token);
        users.Update(token.User);

        // Retire any other outstanding link too: a reset is the moment to close every door
        // that was left open, not just the one that was walked through.
        await tokens.InvalidateOutstandingAsync(token.UserId, now, ct);
        await tokens.SaveChangesAsync(ct);
    }

    // 32 bytes of CSPRNG output, URL-safe so it survives being pasted out of a mail client.
    private static string GenerateToken() =>
        Base64Url(RandomNumberGenerator.GetBytes(32));

    private static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
