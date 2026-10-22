using EduCore.Data;
using EduCore.Enums;
using EduCore.Models;
using EduCore.Security;
using EduCore.Security.Encryption;
using EduCore.Services.Email;
using EduCore.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace EduCore.Tests;

// "I turned two-step sign-in on and it emailed me that my password had changed."
//
// DbSet.Update marks every property modified whether or not its value moved, so the interceptor
// cannot trust IsModified on its own. It reads the change tracker and nothing else, which is
// what lets these run without a database.
public class AccountMailInterceptorTests
{
    private sealed class NoOneInParticular : ICurrentUser
    {
        public Guid UserId => Guid.Empty;
        public UserRole? Role => null;
        public Guid? SchoolId => null;
        public IReadOnlyCollection<Guid> SchoolIds => [];
        public Guid? TeacherId => null;
        public Guid? StudentId => null;
        public Guid? CityId => null;
        public Guid? StateId => null;
        public Guid? GuardianId => null;
        public bool IsStateAdmin => false;
        public bool IsRegionalAdmin => false;
        public bool IsDirectorate => false;
        public bool IsTeacher => false;
        public bool IsStudent => false;
        public bool IsGuardian => false;
    }

    private sealed class PlaintextEncryptor : IFieldEncryptor
    {
        public string Encrypt(string value) => value;
        public string Decrypt(string value) => value;
        public string Seal(string value) => value;
        public bool NeedsReencryption(string? stored) => false;
    }

    private sealed class Spool : IAccountMailQueue
    {
        public List<AccountMailRequest> Sent { get; } = [];

        public void Enqueue(AccountMailRequest request) => Sent.Add(request);

        public IAsyncEnumerable<AccountMailRequest> ReadAllAsync(CancellationToken ct) =>
            throw new NotSupportedException();
    }

    // The hash only has to be a value that changes when the password does.
    private sealed class Hasher : IPasswordHasher<User>
    {
        public string HashPassword(User user, string password) => $"hash:{password}";

        public PasswordVerificationResult VerifyHashedPassword(
            User user, string hashedPassword, string providedPassword) =>
            throw new NotSupportedException();
    }

    private static AppDbContext Context()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=none;Username=none;Password=none;Timeout=1")
            .Options;

        return new AppDbContext(options, new NoOneInParticular(), new PlaintextEncryptor());
    }

    private static User Existing()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Teacher",
            LastName = "Berisha",
            Email = "teacher202@shkolla.example",
            Role = UserRole.Teacher
        };

        user.SetPassword("the-one-they-already-have", new Hasher());

        return user;
    }

    // Runs the interceptor over whatever the change tracker holds, the way a save would.
    private static List<AccountMailRequest> Queued(AppDbContext context)
    {
        var spool = new Spool();
        var interceptor = new AccountMailInterceptor(spool);

        interceptor.SavingChanges(new DbContextEventData(null!, null!, context), default);
        interceptor.SavedChanges(new SaveChangesCompletedEventData(null!, null!, context, 1), 1);

        return spool.Sent;
    }

    [Fact]
    public void Saving_an_unrelated_change_does_not_announce_a_password_change()
    {
        using var context = Context();

        var user = Existing();
        context.Attach(user);

        user.TwoFactorEnabled = true;

        // What the service does: marks the whole entity, hash included, though only the flag moved.
        context.Users.Update(user);

        Assert.Empty(Queued(context));
    }

    [Fact]
    public void Replacing_the_hash_announces_a_password_change()
    {
        using var context = Context();

        var user = Existing();
        context.Attach(user);

        user.SetPassword("a-brand-new-one", new Hasher());
        context.Users.Update(user);

        var request = Assert.Single(Queued(context));

        Assert.Equal(AccountMailKind.PasswordChanged, request.Kind);
        Assert.Equal(user.Email, request.Email);
    }

    [Fact]
    public void A_new_account_is_welcomed()
    {
        using var context = Context();

        context.Users.Add(Existing());

        Assert.Equal(AccountMailKind.Welcome, Assert.Single(Queued(context)).Kind);
    }

    [Fact]
    public void A_deactivated_account_is_left_alone()
    {
        using var context = Context();

        var user = Existing();
        user.DeactivatedAt = DateTime.UtcNow;
        context.Attach(user);

        user.SetPassword("a-brand-new-one", new Hasher());
        context.Users.Update(user);

        Assert.Empty(Queued(context));
    }
}
