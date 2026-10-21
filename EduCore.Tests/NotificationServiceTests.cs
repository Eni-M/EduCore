using EduCore.Dtos.Pagination;
using EduCore.Enums;
using EduCore.Helpers;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using EduCore.Security;
using EduCore.Services;
using Xunit;

namespace EduCore.Tests;

public class NotificationServiceTests
{
    private static readonly Guid SchoolId = Guid.NewGuid();

    private sealed class FakeCurrentUser(UserRole role) : ICurrentUser
    {
        public Guid UserId => Guid.NewGuid();
        public UserRole? Role => role;
        public Guid? SchoolId => NotificationServiceTests.SchoolId;
        public IReadOnlyCollection<Guid> SchoolIds => [NotificationServiceTests.SchoolId];
        public Guid? TeacherId => null;
        public Guid? StudentId => null;
        public Guid? CityId => null;
        public Guid? StateId => null;
        public Guid? GuardianId => null;
        public bool IsStateAdmin => role == UserRole.StateAdmin;
        public bool IsRegionalAdmin => role == UserRole.RegionalAdmin;
        public bool IsDirectorate => role == UserRole.Directorate;
        public bool IsTeacher => role == UserRole.Teacher;
        public bool IsStudent => role == UserRole.Student;
        public bool IsGuardian => role == UserRole.Guardian;
    }

    private sealed class FakeNotificationRepository : INotificationRepository
    {
        public readonly List<Notification> Store = [];
        public int Saves;

        public Task<PagedResultDto<Notification>> GetPagedForRoleAsync(
            UserRole role, bool unreadOnly, PaginationQueryDto pagination, CancellationToken ct) =>
            Task.FromResult(Store.Where(n => n.RecipientRole == role && (!unreadOnly || n.ReadAt == null))
                .OrderByDescending(n => n.CreatedAt).ToPagedResult(pagination));

        public Task<int> CountUnreadForRoleAsync(UserRole role, CancellationToken ct) =>
            Task.FromResult(Store.Count(n => n.RecipientRole == role && n.ReadAt == null));

        public Task<int> MarkAllReadForRoleAsync(UserRole role, DateTime readAt, CancellationToken ct)
        {
            var unread = Store.Where(n => n.RecipientRole == role && n.ReadAt == null).ToList();
            unread.ForEach(n => n.ReadAt = readAt);
            return Task.FromResult(unread.Count);
        }

        public Task<Notification?> GetByIdAsync(Guid id, CancellationToken ct) =>
            Task.FromResult(Store.FirstOrDefault(n => n.Id == id));

        public Task AddAsync(Notification entity) { entity.Id = Guid.NewGuid(); Store.Add(entity); return Task.CompletedTask; }
        public void Update(Notification entity) { }
        public void Remove(Notification entity) => Store.Remove(entity);
        public Task<bool> SaveChangesAsync(CancellationToken ct) { Saves++; return Task.FromResult(true); }
    }

    [Fact]
    public async Task NotifyRole_stores_a_notification_for_that_role()
    {
        var repo = new FakeNotificationRepository();
        var service = new NotificationService(repo, new FakeCurrentUser(UserRole.Directorate), new NotificationSignals());

        await service.NotifyRoleAsync(SchoolId, UserRole.Directorate, "Hello", "/students/1", CancellationToken.None);

        var stored = Assert.Single(repo.Store);
        Assert.Equal(UserRole.Directorate, stored.RecipientRole);
        Assert.Equal("Hello", stored.Message);
        Assert.Equal("/students/1", stored.Link);
        Assert.Null(stored.ReadAt);
        Assert.True(repo.Saves > 0);
    }

    [Fact]
    public async Task GetMine_returns_only_the_callers_role()
    {
        var repo = new FakeNotificationRepository();
        repo.Store.Add(new Notification { Id = Guid.NewGuid(), RecipientRole = UserRole.Directorate, Message = "For office" });
        repo.Store.Add(new Notification { Id = Guid.NewGuid(), RecipientRole = UserRole.Teacher, Message = "For teacher" });

        var service = new NotificationService(repo, new FakeCurrentUser(UserRole.Directorate), new NotificationSignals());

        var mine = (await service.GetMineAsync(false, new PaginationQueryDto(), CancellationToken.None)).Items;

        Assert.Single(mine);
        Assert.Equal("For office", mine[0].Message);
    }

    [Fact]
    public async Task MarkRead_sets_ReadAt()
    {
        var repo = new FakeNotificationRepository();
        var n = new Notification { Id = Guid.NewGuid(), RecipientRole = UserRole.Directorate, Message = "x" };
        repo.Store.Add(n);

        var service = new NotificationService(repo, new FakeCurrentUser(UserRole.Directorate), new NotificationSignals());

        await service.MarkReadAsync(n.Id, CancellationToken.None);

        Assert.NotNull(n.ReadAt);
    }

    [Fact]
    public async Task Writes_wake_only_the_streams_of_that_role_and_school()
    {
        var repo = new FakeNotificationRepository();
        var signals = new NotificationSignals();
        var office = signals.Subscribe(UserRole.Directorate, [SchoolId]);
        var teacher = signals.Subscribe(UserRole.Teacher, [SchoolId]);
        var otherSchool = signals.Subscribe(UserRole.Directorate, [Guid.NewGuid()]);
        var service = new NotificationService(repo, new FakeCurrentUser(UserRole.Directorate), signals);

        await service.NotifyRoleAsync(SchoolId, UserRole.Directorate, "Hello", null, CancellationToken.None);

        Assert.True(office.Reader.TryRead(out _));
        Assert.False(teacher.Reader.TryRead(out _));
        Assert.False(otherSchool.Reader.TryRead(out _));

        await service.MarkReadAsync(repo.Store[0].Id, CancellationToken.None);
        Assert.True(office.Reader.TryRead(out _));

        // Already read: nothing changed, nobody is woken.
        await service.MarkAllReadAsync(CancellationToken.None);
        Assert.False(office.Reader.TryRead(out _));

        signals.Unsubscribe(office);
        await service.NotifyRoleAsync(SchoolId, UserRole.Directorate, "Again", null, CancellationToken.None);
        Assert.False(office.Reader.TryRead(out _));
    }

    [Fact]
    public async Task MarkAllRead_clears_the_callers_role_only()
    {
        var repo = new FakeNotificationRepository();
        repo.Store.Add(new Notification { Id = Guid.NewGuid(), RecipientRole = UserRole.Directorate, Message = "a" });
        repo.Store.Add(new Notification { Id = Guid.NewGuid(), RecipientRole = UserRole.Teacher, Message = "b" });
        var service = new NotificationService(repo, new FakeCurrentUser(UserRole.Directorate), new NotificationSignals());

        await service.MarkAllReadAsync(CancellationToken.None);

        Assert.Equal(0, await service.GetUnreadCountAsync(CancellationToken.None));
        Assert.Null(repo.Store.Single(n => n.RecipientRole == UserRole.Teacher).ReadAt);
    }
}
