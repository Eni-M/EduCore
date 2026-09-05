using EduCore.Endpoints;
using EduCore.Enums;
using EduCore.Exceptions;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using EduCore.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace EduCore.Tests;

// Two gates, deliberately not one. A JWT lives for an hour, so deactivating an account only
// takes effect because these filters re-read the user on every request -- which is why /me had
// to get one too. But the password-rotation gate must NOT reach the endpoints a mid-rotation
// user needs, or changing the password becomes impossible.
public class AccountStatusFilterTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public Guid UserId => AccountStatusFilterTests.UserId;
        public UserRole? Role => UserRole.Teacher;
        public Guid? SchoolId => Guid.Empty;
        public IReadOnlyCollection<Guid> SchoolIds => [Guid.Empty];
        public Guid? TeacherId => null;
        public Guid? StudentId => null;
        public Guid? CityId => null;
        public Guid? StateId => null;
        public Guid? GuardianId => null;
        public bool IsStateAdmin => false;
        public bool IsRegionalAdmin => false;
        public bool IsDirectorate => false;
        public bool IsTeacher => true;
        public bool IsStudent => false;
        public bool IsGuardian => false;
    }

    private sealed class FakeUserRepository(User? user) : IUserRepository
    {
        public Task<User?> GetByIdAsync(Guid id, CancellationToken ct) => Task.FromResult(user);
        public Task<User?> GetByEmailAsync(string email, CancellationToken ct) => Task.FromResult<User?>(null);
        public Task<bool> ExistsByEmailAsync(string email, CancellationToken ct) => Task.FromResult(false);
        public Task AddAsync(User entity) => Task.CompletedTask;
        public void Update(User entity) { }
        public void Remove(User entity) { }
        public Task<bool> SaveChangesAsync(CancellationToken ct) => Task.FromResult(true);
    }

    private static User Account(bool deactivated = false, bool mustChangePassword = false) => new()
    {
        Id = UserId,
        DeactivatedAt = deactivated ? DateTime.UtcNow : null,
        MustChangePassword = mustChangePassword
    };

    // Runs a filter and reports whether the endpoint behind it was actually reached.
    private static async Task<bool> ReachedEndpointAsync(IEndpointFilter filter)
    {
        var reached = false;
        var context = EndpointFilterInvocationContext.Create(new DefaultHttpContext());

        await filter.InvokeAsync(context, _ =>
        {
            reached = true;
            return ValueTask.FromResult<object?>(null);
        });

        return reached;
    }

    // The real cache over the fake repository, not a stub of it: the cache is now part of the
    // revocation path, so a test that replaced it would no longer cover what actually runs.
    private static IAccountStatusCache Accounts(User? user) =>
        new AccountStatusCache(new MemoryCache(new MemoryCacheOptions()), new FakeUserRepository(user));

    private static ActiveAccountEndpointFilter Revocation(User? user) =>
        new(Accounts(user), new FakeCurrentUser());

    private static AccountStatusEndpointFilter Full(User? user) =>
        new(Accounts(user), new FakeCurrentUser());

    // -- revocation gate (/api/auth/me, PUT /me, change-password) --------------------

    [Fact]
    public async Task Revocation_rejects_a_deactivated_account()
    {
        var filter = Revocation(Account(deactivated: true));

        await Assert.ThrowsAsync<UnauthorizedException>(() => ReachedEndpointAsync(filter));
    }

    [Fact]
    public async Task Revocation_rejects_a_token_whose_user_no_longer_exists()
    {
        var filter = Revocation(null);

        await Assert.ThrowsAsync<UnauthorizedException>(() => ReachedEndpointAsync(filter));
    }

    // The whole reason for the split: rotation has to stay reachable.
    [Fact]
    public async Task Revocation_lets_a_must_change_password_account_through()
    {
        var filter = Revocation(Account(mustChangePassword: true));

        Assert.True(await ReachedEndpointAsync(filter));
    }

    [Fact]
    public async Task Revocation_lets_a_normal_account_through()
    {
        var filter = Revocation(Account());

        Assert.True(await ReachedEndpointAsync(filter));
    }

    // -- full gate (the /api group) --------------------------------------------------

    [Fact]
    public async Task Full_gate_rejects_a_deactivated_account()
    {
        var filter = Full(Account(deactivated: true));

        await Assert.ThrowsAsync<UnauthorizedException>(() => ReachedEndpointAsync(filter));
    }

    [Fact]
    public async Task Full_gate_rejects_a_must_change_password_account()
    {
        var filter = Full(Account(mustChangePassword: true));

        await Assert.ThrowsAsync<ForbiddenException>(() => ReachedEndpointAsync(filter));
    }

    // Deactivation outranks the password gate: a deactivated account gets 401, not a 403 that
    // would invite it to try changing its password.
    [Fact]
    public async Task Full_gate_reports_deactivation_before_the_password_gate()
    {
        var filter = Full(Account(deactivated: true, mustChangePassword: true));

        await Assert.ThrowsAsync<UnauthorizedException>(() => ReachedEndpointAsync(filter));
    }

    [Fact]
    public async Task Full_gate_lets_a_normal_account_through()
    {
        var filter = Full(Account());

        Assert.True(await ReachedEndpointAsync(filter));
    }
}
