using EduCore.Data;
using EduCore.Enums;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Repositories;

public class NotificationRepository(AppDbContext context)
    : Repository<Notification>(context), INotificationRepository
{
    public async Task<IReadOnlyList<Notification>> GetForRoleAsync(
        UserRole role, bool unreadOnly, CancellationToken ct) =>
        await _context.Notifications
            .AsNoTracking()
            .Where(n => n.RecipientRole == role && (!unreadOnly || n.ReadAt == null))
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(ct);

    public async Task<int> CountUnreadForRoleAsync(UserRole role, CancellationToken ct) =>
        await _context.Notifications
            .CountAsync(n => n.RecipientRole == role && n.ReadAt == null, ct);
}
