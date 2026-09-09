using EduCore.Dtos.User;
using EduCore.Exceptions;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using EduCore.Services.Account;
using EduCore.Services.Email;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace EduCore.Tests;

// Self-service password reset is an unauthenticated endpoint that can change a password, so
// its security properties are the whole feature: it must not reveal who holds an account, the
// stored token must be useless to anyone who reads the database, and a link must work once.
public class PasswordResetServiceTests
{
    private const string KnownEmail = "teacher@example.com";

    private static PasswordResetOptions ResetOptions() => new()
    {
        TokenLifetimeMinutes = 60,
        ResetUrlTemplate = "https://educore.example/reset-password"
    };

    private static User ActiveUser() => new()
    {
        Id = Guid.NewGuid(),
        FirstName = "Ana",
        LastName = "Berisha",
        Email = KnownEmail,
        MustChangePassword = true
    };

    private static PasswordResetService Build(
        FakeUsers users, FakeTokens tokens, RecordingEmailSender email) =>
        new(users, tokens, new PasswordHasher<User>(), email,
            Options.Create(ResetOptions()), NullLogger<PasswordResetService>.Instance);

    // --- requesting a link ---------------------------------------------------------------

    [Fact]
    public async Task Requesting_a_reset_for_a_known_address_sends_a_link()
    {
        var users = new FakeUsers(ActiveUser());
        var tokens = new FakeTokens();
        var email = new RecordingEmailSender();

        await Build(users, tokens, email).RequestAsync(new ForgotPasswordDto(KnownEmail), CancellationToken.None);

        Assert.Single(email.Sent);
        Assert.Equal(KnownEmail, email.Sent[0].To);
        Assert.Contains("https://educore.example/reset-password?token=", email.Sent[0].Body);
    }

    [Fact]
    public async Task Requesting_a_reset_for_an_unknown_address_succeeds_silently()
    {
        var users = new FakeUsers(null);
        var tokens = new FakeTokens();
        var email = new RecordingEmailSender();

        // No throw and no mail. Anything else — an exception, a 404, a different response —
        // would let anyone test which email addresses hold accounts.
        await Build(users, tokens, email).RequestAsync(new ForgotPasswordDto("nobody@example.com"), CancellationToken.None);

        Assert.Empty(email.Sent);
        Assert.Empty(tokens.Saved);
    }

    [Fact]
    public async Task A_deactivated_account_gets_no_reset_link()
    {
        var user = ActiveUser();
        user.DeactivatedAt = DateTime.UtcNow;

        var users = new FakeUsers(user);
        var tokens = new FakeTokens();
        var email = new RecordingEmailSender();

        await Build(users, tokens, email).RequestAsync(new ForgotPasswordDto(KnownEmail), CancellationToken.None);

        // Otherwise a revoked account could be reopened by whoever still reads that mailbox.
        Assert.Empty(email.Sent);
    }

    [Fact]
    public async Task The_stored_token_is_a_hash_and_never_the_value_that_was_emailed()
    {
        var users = new FakeUsers(ActiveUser());
        var tokens = new FakeTokens();
        var email = new RecordingEmailSender();

        await Build(users, tokens, email).RequestAsync(new ForgotPasswordDto(KnownEmail), CancellationToken.None);

        var emailed = TokenFrom(email.Sent[0].Body);
        var stored = Assert.Single(tokens.Saved).TokenHash;

        Assert.NotEqual(emailed, stored);
        Assert.DoesNotContain(emailed, stored);
        // SHA-256, hex-encoded: anyone reading the table holds a digest, not a working link.
        Assert.Equal(64, stored.Length);
    }

    [Fact]
    public async Task Requesting_a_second_link_retires_the_first()
    {
        var users = new FakeUsers(ActiveUser());
        var tokens = new FakeTokens();
        var email = new RecordingEmailSender();

        var service = Build(users, tokens, email);

        await service.RequestAsync(new ForgotPasswordDto(KnownEmail), CancellationToken.None);
        await service.RequestAsync(new ForgotPasswordDto(KnownEmail), CancellationToken.None);

        // A stale link sitting in an inbox must stop working once a newer one is issued.
        Assert.Equal(2, tokens.InvalidateCalls);
    }

    // --- redeeming a link -----------------------------------------------------------------

    [Fact]
    public async Task Redeeming_a_valid_token_sets_the_password_and_clears_the_forced_change()
    {
        var user = ActiveUser();
        var users = new FakeUsers(user);
        var tokens = new FakeTokens();
        var email = new RecordingEmailSender();

        var service = Build(users, tokens, email);
        await service.RequestAsync(new ForgotPasswordDto(KnownEmail), CancellationToken.None);

        var emailed = TokenFrom(email.Sent[0].Body);
        tokens.Live = tokens.Saved[0];
        tokens.Live.User = user;

        await service.ResetAsync(new ResetPasswordDto(emailed, "NewPassw0rd", "NewPassw0rd"), CancellationToken.None);

        Assert.NotNull(tokens.Live.UsedAt);
        // The user proved control of the mailbox and chose the password, so there is nothing
        // left to force at next sign-in.
        Assert.False(user.MustChangePassword);
        Assert.Equal(
            PasswordVerificationResult.Success,
            new PasswordHasher<User>().VerifyHashedPassword(user, user.PasswordHash, "NewPassw0rd"));
    }

    [Fact]
    public async Task An_unknown_token_is_rejected()
    {
        var service = Build(new FakeUsers(ActiveUser()), new FakeTokens(), new RecordingEmailSender());

        await Assert.ThrowsAsync<BusinessException>(
            () => service.ResetAsync(new ResetPasswordDto("nonsense", "NewPassw0rd", "NewPassw0rd"), CancellationToken.None));
    }

    [Fact]
    public async Task An_expired_token_is_rejected()
    {
        var user = ActiveUser();
        var tokens = new FakeTokens
        {
            Live = new PasswordResetToken
            {
                UserId = user.Id,
                User = user,
                TokenHash = "irrelevant",
                ExpiresAt = DateTime.UtcNow.AddMinutes(-1)
            }
        };

        var service = Build(new FakeUsers(user), tokens, new RecordingEmailSender());

        await Assert.ThrowsAsync<BusinessException>(
            () => service.ResetAsync(new ResetPasswordDto("anything", "NewPassw0rd", "NewPassw0rd"), CancellationToken.None));
    }

    [Fact]
    public async Task An_already_used_token_is_rejected()
    {
        var user = ActiveUser();
        var tokens = new FakeTokens
        {
            Live = new PasswordResetToken
            {
                UserId = user.Id,
                User = user,
                TokenHash = "irrelevant",
                ExpiresAt = DateTime.UtcNow.AddMinutes(30),
                UsedAt = DateTime.UtcNow.AddMinutes(-5)
            }
        };

        var service = Build(new FakeUsers(user), tokens, new RecordingEmailSender());

        // Single use: a link forwarded or recovered from a mail archive must not work twice.
        await Assert.ThrowsAsync<BusinessException>(
            () => service.ResetAsync(new ResetPasswordDto("anything", "NewPassw0rd", "NewPassw0rd"), CancellationToken.None));
    }

    [Fact]
    public async Task A_token_belonging_to_an_account_deactivated_after_it_was_issued_is_rejected()
    {
        var user = ActiveUser();
        user.DeactivatedAt = DateTime.UtcNow;

        var tokens = new FakeTokens
        {
            Live = new PasswordResetToken
            {
                UserId = user.Id,
                User = user,
                TokenHash = "irrelevant",
                ExpiresAt = DateTime.UtcNow.AddMinutes(30)
            }
        };

        var service = Build(new FakeUsers(user), tokens, new RecordingEmailSender());

        await Assert.ThrowsAsync<BusinessException>(
            () => service.ResetAsync(new ResetPasswordDto("anything", "NewPassw0rd", "NewPassw0rd"), CancellationToken.None));
    }

    [Fact]
    public async Task Unknown_expired_and_used_tokens_all_report_the_same_message()
    {
        var user = ActiveUser();

        var unknown = await Assert.ThrowsAsync<BusinessException>(() =>
            Build(new FakeUsers(user), new FakeTokens(), new RecordingEmailSender())
                .ResetAsync(new ResetPasswordDto("a", "NewPassw0rd", "NewPassw0rd"), CancellationToken.None));

        var expired = await Assert.ThrowsAsync<BusinessException>(() =>
            Build(new FakeUsers(user),
                    new FakeTokens
                    {
                        Live = new PasswordResetToken
                        {
                            UserId = user.Id, User = user, TokenHash = "h",
                            ExpiresAt = DateTime.UtcNow.AddMinutes(-1)
                        }
                    },
                    new RecordingEmailSender())
                .ResetAsync(new ResetPasswordDto("a", "NewPassw0rd", "NewPassw0rd"), CancellationToken.None));

        // Distinguishing them would tell someone holding a guessed token which half of the
        // guess was right.
        Assert.Equal(unknown.Message, expired.Message);
    }

    private static string TokenFrom(string body)
    {
        var marker = "?token=";
        var start = body.IndexOf(marker, StringComparison.Ordinal) + marker.Length;
        var end = body.IndexOfAny(['\r', '\n', ' '], start);

        return Uri.UnescapeDataString(end < 0 ? body[start..] : body[start..end]);
    }

    // --- fakes -----------------------------------------------------------------------------

    private sealed class FakeUsers(User? user) : IUserRepository
    {
        public Task<User?> GetByEmailAsync(string email, CancellationToken ct) =>
            Task.FromResult(user is not null && user.Email == email ? user : null);

        public Task<bool> ExistsByEmailAsync(string email, CancellationToken ct) =>
            Task.FromResult(user is not null && user.Email == email);

        public Task<User?> GetByIdAsync(Guid id, CancellationToken ct) => Task.FromResult(user);
        public Task AddAsync(User entity) => Task.CompletedTask;
        public void Update(User entity) { }
        public void Remove(User entity) { }
        public Task<bool> SaveChangesAsync(CancellationToken ct) => Task.FromResult(true);
    }

    private sealed class FakeTokens : IPasswordResetTokenRepository
    {
        public List<PasswordResetToken> Saved { get; } = [];
        public PasswordResetToken? Live { get; set; }
        public int InvalidateCalls { get; private set; }

        public Task<PasswordResetToken?> GetByHashAsync(string tokenHash, CancellationToken ct) =>
            Task.FromResult(Live);

        public Task InvalidateOutstandingAsync(Guid userId, DateTime nowUtc, CancellationToken ct)
        {
            InvalidateCalls++;
            return Task.CompletedTask;
        }

        public Task<PasswordResetToken?> GetByIdAsync(Guid id, CancellationToken ct) =>
            Task.FromResult<PasswordResetToken?>(null);

        public Task AddAsync(PasswordResetToken entity)
        {
            Saved.Add(entity);
            return Task.CompletedTask;
        }

        public void Update(PasswordResetToken entity) { }
        public void Remove(PasswordResetToken entity) { }
        public Task<bool> SaveChangesAsync(CancellationToken ct) => Task.FromResult(true);
    }

    private sealed class RecordingEmailSender : IEmailSender
    {
        public List<(string To, string Subject, string Body)> Sent { get; } = [];

        public Task SendAsync(string toEmail, string subject, string body, CancellationToken ct)
        {
            Sent.Add((toEmail, subject, body));
            return Task.CompletedTask;
        }
    }
}
