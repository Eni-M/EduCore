using EduCore.Enums;
using EduCore.Exceptions;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using EduCore.Security;
using EduCore.Services;
using EduCore.Services.Account;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Xunit;

namespace EduCore.Tests;

// The current password guards removing an authenticator, switching two-step sign-in off and the
// rest. A borrowed session must not be able to guess it, so wrong ones are capped per user.
public class CurrentPasswordThrottleTests
{
    private sealed class OneUser(User user) : IUserRepository
    {
        public Task<User?> GetByIdAsync(Guid id, CancellationToken ct) => Task.FromResult(id == user.Id ? user : null);
        public Task<User?> GetByEmailAsync(string email, CancellationToken ct) => Task.FromResult<User?>(null);
        public Task<bool> ExistsByEmailAsync(string email, CancellationToken ct) => Task.FromResult(false);
        public Task AddAsync(User entity) => Task.CompletedTask;
        public void Update(User entity) { }
        public void Remove(User entity) { }
        public Task<bool> SaveChangesAsync(CancellationToken ct) => Task.FromResult(true);
    }

    [Fact]
    public async Task Five_wrong_passwords_stop_even_the_right_one_for_the_window()
    {
        var hasher = new PasswordHasher<User>();
        var user = new User { Id = Guid.NewGuid(), Email = "drejtoria@shkolla.example", Role = UserRole.Teacher };
        user.SetPassword("the-real-pass-phrase", hasher);
        var ct = TestContext.Current.CancellationToken;

        // Only what RemoveAuthenticatorAsync touches is real; the rest is never reached.
        var users = new UserService(
            new OneUser(user), null!, null!, null!, null!, null!, null!, null!, hasher, null!,
            Options.Create(new TwoFactorOptions()), null!, null!, null!,
            new AttemptThrottle(new MemoryCache(new MemoryCacheOptions())), null!, null!);

        for (var i = 0; i < 5; i++)
            await Assert.ThrowsAsync<BusinessException>(() => users.RemoveAuthenticatorAsync(user.Id, "a-guess", ct));

        await Assert.ThrowsAsync<TooManyRequestsException>(() =>
            users.RemoveAuthenticatorAsync(user.Id, "the-real-pass-phrase", ct));
    }
}
