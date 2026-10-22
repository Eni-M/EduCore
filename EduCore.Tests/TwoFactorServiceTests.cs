using EduCore.Enums;
using EduCore.Exceptions;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using EduCore.Services.Account;
using EduCore.Services.Email;
using Microsoft.Extensions.Options;
using Xunit;
using EduCore.Services.Interfaces;

namespace EduCore.Tests;

// A six-digit code is a million possibilities, which is only out of reach because the attempt
// count is capped and the window is minutes. Those two are the security of this feature, not
// housekeeping, so they are what these tests hold down.
public class TwoFactorServiceTests
{
    private sealed class FakeChallenges : ITwoFactorChallengeRepository
    {
        public readonly List<TwoFactorChallenge> Rows = [];

        public Task<TwoFactorChallenge?> GetOpenAsync(Guid id, CancellationToken ct) =>
            Task.FromResult(Rows.FirstOrDefault(c => c.Id == id && c.CompletedAt is null));

        public Task InvalidateOutstandingAsync(
            Guid userId, TwoFactorPurpose purpose, DateTime nowUtc, CancellationToken ct)
        {
            foreach (var c in Rows.Where(c => c.UserId == userId && c.Purpose == purpose && c.CompletedAt is null))
                c.CompletedAt = nowUtc;
            return Task.CompletedTask;
        }

        public Task AddAsync(TwoFactorChallenge entity)
        {
            if (entity.Id == Guid.Empty) entity.Id = Guid.NewGuid();
            Rows.Add(entity);
            return Task.CompletedTask;
        }

        public Task<TwoFactorChallenge?> GetByIdAsync(Guid id, CancellationToken ct) =>
            Task.FromResult(Rows.FirstOrDefault(c => c.Id == id));
        public Task<IEnumerable<TwoFactorChallenge>> GetAllAsync(CancellationToken ct) =>
            Task.FromResult<IEnumerable<TwoFactorChallenge>>(Rows);
        public void Update(TwoFactorChallenge entity) { }
        public void Remove(TwoFactorChallenge entity) => Rows.Remove(entity);
        public Task<bool> SaveChangesAsync(CancellationToken ct) => Task.FromResult(true);
    }

    private sealed class CapturingEmail : IEmailSender
    {
        public readonly List<(string To, string Subject, EmailBody Body)> Sent = [];

        public Task SendAsync(string to, string subject, EmailBody body, CancellationToken ct)
        {
            Sent.Add((to, subject, body));
            return Task.CompletedTask;
        }
    }

    private static User Admin() => new()
    {
        Id = Guid.NewGuid(), Email = "drejtoria@shkolla.example",
        FirstName = "Ana", LastName = "Berisha", Role = UserRole.Directorate
    };

    private static User Student() => new()
    {
        Id = Guid.NewGuid(), Email = "nxenes@shkolla.example",
        FirstName = "Ben", LastName = "Krasniqi", Role = UserRole.Student
    };

    private static (TwoFactorService Service, FakeChallenges Rows, CapturingEmail Mail) Build(
        int maxAttempts = 5, int lifetimeMinutes = 10)
    {
        var rows = new FakeChallenges();
        var mail = new CapturingEmail();

        var service = new TwoFactorService(
            rows, mail,
            Options.Create(new TwoFactorOptions { MaxAttempts = maxAttempts, CodeLifetimeMinutes = lifetimeMinutes }),
            Options.Create(new PasswordResetOptions { ResetUrlTemplate = "https://educore.example/reset-password" }),
            new NullAuditTrail());

        return (service, rows, mail);
    }

    // The code only exists in the email, so that is where a test has to read it from. The
    // Code block is rendered on a line of its own, which is what makes it findable -- scraping
    // every digit in the body would also pick up the "expires in 10 minutes".
    private static string CodeFrom(CapturingEmail mail) =>
        mail.Sent[^1].Body.Text
            .Split('\n')
            .Select(l => l.Trim())
            .First(l => l.Length == 6 && l.All(char.IsDigit));

    // ---- who has to use it -------------------------------------------------

    [Theory]
    [InlineData(UserRole.Directorate)]
    [InlineData(UserRole.RegionalAdmin)]
    [InlineData(UserRole.StateAdmin)]
    public void The_roles_that_manage_accounts_always_need_a_second_step(UserRole role)
    {
        var (service, _, _) = Build();
        var user = Admin();
        user.Role = role;
        user.TwoFactorEnabled = false;

        Assert.True(service.SignInRequiresSecondStep(user));
    }

    [Fact]
    public void Everyone_else_only_needs_one_if_they_asked_for_it()
    {
        var (service, _, _) = Build();
        var user = Student();

        Assert.False(service.SignInRequiresSecondStep(user));

        user.TwoFactorEnabled = true;
        Assert.True(service.SignInRequiresSecondStep(user));
    }

    // ---- issuing -----------------------------------------------------------

    [Fact]
    public async Task Issuing_emails_a_six_digit_code_and_stores_only_its_hash()
    {
        var (service, rows, mail) = Build();
        var user = Admin();

        await service.IssueAsync(user, TwoFactorPurpose.SignIn, TestContext.Current.CancellationToken);

        Assert.Single(mail.Sent);
        Assert.Equal(user.Email, mail.Sent[0].To);

        var code = CodeFrom(mail);
        Assert.Equal(6, code.Length);

        // The plaintext code must exist in the inbox and nowhere else.
        Assert.DoesNotContain(code, rows.Rows[0].CodeHash);
    }

    [Fact]
    public async Task Asking_for_a_new_code_retires_the_previous_one()
    {
        var (service, rows, mail) = Build();
        var user = Admin();

        var first = await service.IssueAsync(user, TwoFactorPurpose.SignIn, TestContext.Current.CancellationToken);
        var firstCode = CodeFrom(mail);

        await service.IssueAsync(user, TwoFactorPurpose.SignIn, TestContext.Current.CancellationToken);

        // A code still sitting in an inbox stops working the moment another is requested.
        rows.Rows[0].User = user;
        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            service.CompleteAsync(first, firstCode, TwoFactorPurpose.SignIn, TestContext.Current.CancellationToken));
    }

    // ---- completing --------------------------------------------------------

    [Fact]
    public async Task The_right_code_completes_the_challenge_once_and_only_once()
    {
        var (service, rows, mail) = Build();
        var user = Admin();

        var id = await service.IssueAsync(user, TwoFactorPurpose.SignIn, TestContext.Current.CancellationToken);
        rows.Rows[0].User = user;
        var code = CodeFrom(mail);

        var completed = await service.CompleteAsync(
            id, code, TwoFactorPurpose.SignIn, TestContext.Current.CancellationToken);
        Assert.Equal(user.Id, completed.Id);

        // Replay must not work.
        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            service.CompleteAsync(id, code, TwoFactorPurpose.SignIn, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task A_challenge_dies_once_the_attempt_cap_is_reached()
    {
        var (service, rows, mail) = Build(maxAttempts: 3);
        var user = Admin();

        var id = await service.IssueAsync(user, TwoFactorPurpose.SignIn, TestContext.Current.CancellationToken);
        rows.Rows[0].User = user;
        var code = CodeFrom(mail);

        for (var i = 0; i < 3; i++)
            await Assert.ThrowsAsync<UnauthorizedException>(() =>
                service.CompleteAsync(id, "000000", TwoFactorPurpose.SignIn, TestContext.Current.CancellationToken));

        // Even the correct code is refused now -- this is what keeps a million-guess space
        // out of reach.
        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            service.CompleteAsync(id, code, TwoFactorPurpose.SignIn, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task An_expired_code_is_refused()
    {
        var (service, rows, mail) = Build(lifetimeMinutes: 10);
        var user = Admin();

        var id = await service.IssueAsync(user, TwoFactorPurpose.SignIn, TestContext.Current.CancellationToken);
        rows.Rows[0].User = user;
        rows.Rows[0].ExpiresAt = DateTime.UtcNow.AddMinutes(-1);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            service.CompleteAsync(id, CodeFrom(mail), TwoFactorPurpose.SignIn, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task A_code_for_one_purpose_cannot_be_spent_on_another()
    {
        // Otherwise a code emailed to finish a sign-in would also authorise a password change,
        // which is the more damaging of the two.
        var (service, rows, mail) = Build();
        var user = Admin();

        var id = await service.IssueAsync(user, TwoFactorPurpose.SignIn, TestContext.Current.CancellationToken);
        rows.Rows[0].User = user;

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            service.CompleteAsync(id, CodeFrom(mail), TwoFactorPurpose.PasswordChange, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task The_code_never_appears_in_the_challenge_row()
    {
        var (service, rows, mail) = Build();

        await service.IssueAsync(Admin(), TwoFactorPurpose.PasswordChange, TestContext.Current.CancellationToken);

        var code = CodeFrom(mail);
        var row = rows.Rows[0];

        Assert.DoesNotContain(code, row.CodeHash);
        Assert.NotEqual(code, row.CodeHash);
    }
}
