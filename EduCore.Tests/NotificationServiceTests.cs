using EduCore.Dtos.Notification;
using EduCore.Dtos.Pagination;
using EduCore.Enums;
using EduCore.Exceptions;
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

    private sealed class FakeCurrentUser(UserRole role, Guid? userId = null) : ICurrentUser
    {
        public Guid UserId { get; } = userId ?? Guid.NewGuid();
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

    // Mirrors the real repository's rules over lists: role-addressed rows, per-user read rows.
    private sealed class FakeNotificationRepository : INotificationRepository
    {
        public readonly List<Notification> Store = [];
        public readonly List<NotificationRead> Reads = [];
        public int Saves;

        private bool IsRead(Guid notificationId, Guid userId) =>
            Reads.Any(r => r.NotificationId == notificationId && r.UserId == userId);

        public Task<PagedResultDto<NotificationDto>> GetPagedForUserAsync(
            UserRole role, Guid userId, bool unreadOnly, PaginationQueryDto pagination, CancellationToken ct) =>
            Task.FromResult(Store
                .Where(n => n.RecipientRole == role && (!unreadOnly || !IsRead(n.Id, userId)))
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new NotificationDto(n.Id, n.Message, n.Link, n.CreatedAt,
                    Reads.FirstOrDefault(r => r.NotificationId == n.Id && r.UserId == userId)?.ReadAt))
                .ToPagedResult(pagination));

        public Task<int> CountUnreadAsync(UserRole role, Guid userId, CancellationToken ct) =>
            Task.FromResult(Store.Count(n => n.RecipientRole == role && !IsRead(n.Id, userId)));

        public Task<int> MarkAllReadAsync(UserRole role, Guid userId, DateTime readAt, CancellationToken ct)
        {
            var unread = Store.Where(n => n.RecipientRole == role && !IsRead(n.Id, userId)).ToList();
            Reads.AddRange(unread.Select(n => new NotificationRead { NotificationId = n.Id, UserId = userId, ReadAt = readAt }));
            return Task.FromResult(unread.Count);
        }

        public Task<bool> IsReadByAsync(Guid notificationId, Guid userId, CancellationToken ct) =>
            Task.FromResult(IsRead(notificationId, userId));

        public void AddRead(NotificationRead read) => Reads.Add(read);

        public Task<Notification?> GetByIdAsync(Guid id, CancellationToken ct) =>
            Task.FromResult(Store.FirstOrDefault(n => n.Id == id));

        public Task AddAsync(Notification entity) { entity.Id = Guid.NewGuid(); Store.Add(entity); return Task.CompletedTask; }
        public void Update(Notification entity) { }
        public void Remove(Notification entity) => Store.Remove(entity);
        public Task<bool> SaveChangesAsync(CancellationToken ct) { Saves++; return Task.FromResult(true); }
    }

    private static Notification ForOffice(string message = "x") =>
        new() { Id = Guid.NewGuid(), SchoolId = SchoolId, RecipientRole = UserRole.Directorate, Message = message, CreatedAt = DateTime.UtcNow };

    // Staged, not saved: the caller's own save commits it together with the change it reports.
    [Fact]
    public async Task StageForRole_adds_without_saving()
    {
        var repo = new FakeNotificationRepository();
        var service = new NotificationService(repo, new FakeCurrentUser(UserRole.Directorate));

        await service.StageForRoleAsync(SchoolId, UserRole.Directorate, "Hello", "/students/1", CancellationToken.None);

        var stored = Assert.Single(repo.Store);
        Assert.Equal(UserRole.Directorate, stored.RecipientRole);
        Assert.Equal("/students/1", stored.Link);
        Assert.Equal(0, repo.Saves);
    }

    [Fact]
    public async Task GetMine_returns_only_the_callers_role()
    {
        var repo = new FakeNotificationRepository();
        repo.Store.Add(ForOffice("For office"));
        repo.Store.Add(new Notification { Id = Guid.NewGuid(), RecipientRole = UserRole.Teacher, Message = "For teacher" });

        var service = new NotificationService(repo, new FakeCurrentUser(UserRole.Directorate));
        var mine = (await service.GetMineAsync(false, new PaginationQueryDto(), CancellationToken.None)).Items;

        Assert.Equal("For office", Assert.Single(mine).Message);
    }

    // The bug this replaced: one office user reading a notification cleared it for the others.
    [Fact]
    public async Task Reading_a_notification_clears_it_for_that_user_only()
    {
        var repo = new FakeNotificationRepository();
        var n = ForOffice();
        repo.Store.Add(n);

        var alice = new NotificationService(repo, new FakeCurrentUser(UserRole.Directorate));
        var bob = new NotificationService(repo, new FakeCurrentUser(UserRole.Directorate));

        await alice.MarkReadAsync(n.Id, CancellationToken.None);

        Assert.Equal(0, await alice.GetUnreadCountAsync(CancellationToken.None));
        Assert.Equal(1, await bob.GetUnreadCountAsync(CancellationToken.None));
    }

    [Fact]
    public async Task MarkRead_is_idempotent()
    {
        var repo = new FakeNotificationRepository();
        var n = ForOffice();
        repo.Store.Add(n);
        var service = new NotificationService(repo, new FakeCurrentUser(UserRole.Directorate));

        await service.MarkReadAsync(n.Id, CancellationToken.None);
        await service.MarkReadAsync(n.Id, CancellationToken.None);

        Assert.Single(repo.Reads);
    }

    [Fact]
    public async Task Another_roles_notification_is_not_found()
    {
        var repo = new FakeNotificationRepository();
        var n = ForOffice();
        repo.Store.Add(n);
        var teacher = new NotificationService(repo, new FakeCurrentUser(UserRole.Teacher));

        await Assert.ThrowsAsync<NotFoundException>(() => teacher.MarkReadAsync(n.Id, CancellationToken.None));
    }

    [Fact]
    public async Task MarkAllRead_clears_the_callers_role_only()
    {
        var repo = new FakeNotificationRepository();
        repo.Store.Add(ForOffice());
        repo.Store.Add(new Notification { Id = Guid.NewGuid(), RecipientRole = UserRole.Teacher, Message = "b" });
        var user = new FakeCurrentUser(UserRole.Directorate);
        var service = new NotificationService(repo, user);

        await service.MarkAllReadAsync(CancellationToken.None);

        Assert.Equal(0, await service.GetUnreadCountAsync(CancellationToken.None));
        Assert.Equal(1, await new NotificationService(repo, new FakeCurrentUser(UserRole.Teacher)).GetUnreadCountAsync(CancellationToken.None));
    }

    [Fact]
    public void Streams_are_woken_only_for_their_role_and_school()
    {
        var signals = new NotificationSignals();
        var office = signals.Subscribe(UserRole.Directorate, [SchoolId]);
        var teacher = signals.Subscribe(UserRole.Teacher, [SchoolId]);
        var otherSchool = signals.Subscribe(UserRole.Directorate, [Guid.NewGuid()]);

        signals.Changed(SchoolId, UserRole.Directorate);

        Assert.True(office.Reader.TryRead(out _));
        Assert.False(teacher.Reader.TryRead(out _));
        Assert.False(otherSchool.Reader.TryRead(out _));

        signals.Unsubscribe(office);
        signals.Changed(SchoolId, UserRole.Directorate);
        Assert.False(office.Reader.TryRead(out _));
    }
}
