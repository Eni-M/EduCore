using EduCore.Models;
using EduCore.Repositories.Interfaces;
using EduCore.Configuration;
using EduCore.Security;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Xunit;

namespace EduCore.Tests;

// The account-status cache sits on the revocation path, so its failure mode is not a slow page
// -- it is a deactivated account that keeps working. These cover the two properties that make
// caching it safe at all: reads are served without touching the database, and an eviction is
// visible immediately rather than after the entry expires.
public class AccountStatusCacheTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    // Counts reads, so a test can prove a lookup was served from cache rather than repeated.
    private sealed class CountingUserRepository(User? user) : IUserRepository
    {
        public int Reads { get; private set; }

        public User? Current { get; set; } = user;

        public Task<User?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            Reads++;
            return Task.FromResult(Current);
        }

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

    private static (AccountStatusCache Cache, CountingUserRepository Users, IMemoryCache Memory) Build(
        User? user)
    {
        var memory = new MemoryCache(new MemoryCacheOptions());
        var users = new CountingUserRepository(user);

        return (new AccountStatusCache(memory, users, Options.Create(new CachingOptions())), users, memory);
    }

    [Fact]
    public async Task First_read_hits_the_database()
    {
        var (cache, users, _) = Build(Account());

        var status = await cache.GetAsync(UserId, CancellationToken.None);

        Assert.Equal(1, users.Reads);
        Assert.True(status.Exists);
        Assert.False(status.IsDeactivated);
        Assert.False(status.MustChangePassword);
    }

    [Fact]
    public async Task Repeated_reads_are_served_from_cache()
    {
        // The whole point: this ran once per authenticated request before.
        var (cache, users, _) = Build(Account());

        for (var i = 0; i < 25; i++)
            await cache.GetAsync(UserId, CancellationToken.None);

        Assert.Equal(1, users.Reads);
    }

    [Fact]
    public async Task Deactivation_is_carried_through()
    {
        var (cache, _, _) = Build(Account(deactivated: true));

        var status = await cache.GetAsync(UserId, CancellationToken.None);

        Assert.True(status.Exists);
        Assert.True(status.IsDeactivated);
    }

    [Fact]
    public async Task A_forced_rotation_is_carried_through()
    {
        var (cache, _, _) = Build(Account(mustChangePassword: true));

        var status = await cache.GetAsync(UserId, CancellationToken.None);

        Assert.True(status.MustChangePassword);
    }

    [Fact]
    public async Task A_user_that_does_not_exist_is_cached_too()
    {
        // A stale token for a deleted account must not be able to read the database on every
        // request it makes -- that would reintroduce exactly the load this cache removes.
        var (cache, users, _) = Build(null);

        await cache.GetAsync(UserId, CancellationToken.None);
        await cache.GetAsync(UserId, CancellationToken.None);

        Assert.Equal(1, users.Reads);
        Assert.False((await cache.GetAsync(UserId, CancellationToken.None)).Exists);
    }

    [Fact]
    public async Task Eviction_makes_a_deactivation_visible_immediately()
    {
        // This is what AccountStatusInvalidationInterceptor does as part of the write that
        // deactivates someone. Without it, revocation would wait out the TTL.
        var (cache, users, memory) = Build(Account());

        Assert.False((await cache.GetAsync(UserId, CancellationToken.None)).IsDeactivated);

        users.Current = Account(deactivated: true);
        memory.Remove(AccountStatusCache.Key(UserId));

        Assert.True((await cache.GetAsync(UserId, CancellationToken.None)).IsDeactivated);
        Assert.Equal(2, users.Reads);
    }

    [Fact]
    public async Task Without_eviction_a_stale_status_is_served()
    {
        // Stated as a test because it is the risk the interceptor exists to remove, and the
        // reason eviction hangs off the write itself rather than off each calling service.
        var (cache, users, _) = Build(Account());

        await cache.GetAsync(UserId, CancellationToken.None);
        users.Current = Account(deactivated: true);

        Assert.False((await cache.GetAsync(UserId, CancellationToken.None)).IsDeactivated);
    }

    [Fact]
    public async Task Users_are_cached_independently()
    {
        var other = Guid.NewGuid();
        var (cache, users, _) = Build(Account());

        await cache.GetAsync(UserId, CancellationToken.None);
        await cache.GetAsync(other, CancellationToken.None);

        Assert.Equal(2, users.Reads);
    }
}
