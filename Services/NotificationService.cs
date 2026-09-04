using EduCore.Dtos.Notification;
using EduCore.Enums;
using EduCore.Exceptions;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using EduCore.Security;
using EduCore.Services.Interfaces;

namespace EduCore.Services;

public class NotificationService(
    INotificationRepository notifications,
    ICurrentUser currentUser) : INotificationService
{
    public async Task NotifyRoleAsync(
        Guid schoolId, UserRole role, string message, string? link, CancellationToken ct)
    {
        await notifications.AddAsync(new Notification
        {
            SchoolId = schoolId,
            RecipientRole = role,
            Message = message,
            Link = link,
            CreatedAt = DateTime.UtcNow,
        });

        await notifications.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<NotificationDto>> GetMineAsync(bool unreadOnly, CancellationToken ct)
    {
        var role = currentUser.Role ?? throw new ForbiddenException("You have no role.");

        return (await notifications.GetForRoleAsync(role, unreadOnly, ct))
            .Select(n => new NotificationDto(n.Id, n.Message, n.Link, n.CreatedAt, n.ReadAt));
    }

    public async Task<int> GetUnreadCountAsync(CancellationToken ct)
    {
        var role = currentUser.Role ?? throw new ForbiddenException("You have no role.");
        return await notifications.CountUnreadForRoleAsync(role, ct);
    }

    public async Task MarkReadAsync(Guid id, CancellationToken ct)
    {
        // The school filter keeps this to notifications in the caller's own school.
        var notification = await notifications.GetByIdAsync(id, ct)
                           ?? throw new NotFoundException("Notification", id);

        if (notification.ReadAt is not null) return;

        notification.ReadAt = DateTime.UtcNow;
        notifications.Update(notification);
        await notifications.SaveChangesAsync(ct);
    }
}
