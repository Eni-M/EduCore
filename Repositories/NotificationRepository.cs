using EduCore.Data;
using EduCore.Dtos.Notification;
using EduCore.Dtos.Pagination;
using EduCore.Enums;
using EduCore.Helpers;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Repositories;

public class NotificationRepository(AppDbContext context)
    : Repository<Notification>(context), INotificationRepository
{
    private IQueryable<Notification> ForRole(UserRole role) =>
        _context.Notifications.AsNoTracking().Where(n => n.RecipientRole == role);

    private IQueryable<Notification> UnreadBy(UserRole role, Guid userId) =>
        ForRole(role).Where(n => !_context.NotificationReads.Any(r => r.NotificationId == n.Id && r.UserId == userId));

    public async Task<PagedResultDto<NotificationDto>> GetPagedForUserAsync(
        UserRole role, Guid userId, bool unreadOnly, PaginationQueryDto pagination, CancellationToken ct)
    {
        var (page, pageSize) = ListPagination.Normalize(pagination);

        var query = unreadOnly ? UnreadBy(role, userId) : ForRole(role);
        var totalCount = await query.CountAsync(ct);

        // Id after CreatedAt so rows written in the same instant keep their order between pages.
        var items = await (
                from n in query
                orderby n.CreatedAt descending, n.Id
                select new NotificationDto(
                    n.Id, n.Message, n.Link, n.CreatedAt,
                    _context.NotificationReads
                        .Where(r => r.NotificationId == n.Id && r.UserId == userId)
                        .Select(r => (DateTime?)r.ReadAt)
                        .FirstOrDefault()))
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResultDto<NotificationDto>(items, page, pageSize, totalCount);
    }

    public async Task<int> CountUnreadAsync(UserRole role, Guid userId, CancellationToken ct) =>
        await UnreadBy(role, userId).CountAsync(ct);

    // The school filter applies, so this only ever marks the caller's own schools' notifications.
    public async Task<int> MarkAllReadAsync(UserRole role, Guid userId, DateTime readAt, CancellationToken ct)
    {
        var unread = await UnreadBy(role, userId).Select(n => new { n.Id, n.SchoolId }).ToListAsync(ct);

        _context.NotificationReads.AddRange(unread.Select(n => new NotificationRead
        {
            NotificationId = n.Id, UserId = userId, ReadAt = readAt, SchoolId = n.SchoolId, RecipientRole = role
        }));

        await _context.SaveChangesAsync(ct);
        return unread.Count;
    }

    public async Task<bool> IsReadByAsync(Guid notificationId, Guid userId, CancellationToken ct) =>
        await _context.NotificationReads.AnyAsync(r => r.NotificationId == notificationId && r.UserId == userId, ct);

    public void AddRead(NotificationRead read) => _context.NotificationReads.Add(read);
}
