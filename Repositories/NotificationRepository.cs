using EduCore.Data;
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
    public async Task<PagedResultDto<Notification>> GetPagedForRoleAsync(
        UserRole role, bool unreadOnly, PaginationQueryDto pagination, CancellationToken ct)
    {
        var (page, pageSize) = ListPagination.Normalize(pagination);

        var query = _context.Notifications
            .AsNoTracking()
            .Where(n => n.RecipientRole == role && (!unreadOnly || n.ReadAt == null));

        var totalCount = await query.CountAsync(ct);

        // Id after CreatedAt so rows written in the same instant keep their order between pages.
        var items = await query
            .OrderByDescending(n => n.CreatedAt).ThenBy(n => n.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResultDto<Notification>(items, page, pageSize, totalCount);
    }

    public async Task<int> CountUnreadForRoleAsync(UserRole role, CancellationToken ct) =>
        await _context.Notifications
            .CountAsync(n => n.RecipientRole == role && n.ReadAt == null, ct);

    // The school filter applies to the bulk update too, so this stays within the caller's schools.
    public async Task<int> MarkAllReadForRoleAsync(UserRole role, DateTime readAt, CancellationToken ct) =>
        await _context.Notifications
            .Where(n => n.RecipientRole == role && n.ReadAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.ReadAt, readAt), ct);
}
