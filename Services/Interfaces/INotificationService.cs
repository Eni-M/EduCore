using EduCore.Dtos.Notification;
using EduCore.Enums;

namespace EduCore.Services.Interfaces;

public interface INotificationService
{
    // Raise a notification for everyone of a role within a school. Saves immediately.
    Task NotifyRoleAsync(Guid schoolId, UserRole role, string message, string? link, CancellationToken ct);

    // The current user's notifications (matched to their role, within their school scope).
    Task<IEnumerable<NotificationDto>> GetMineAsync(bool unreadOnly, CancellationToken ct);
    Task<int> GetUnreadCountAsync(CancellationToken ct);
    Task MarkReadAsync(Guid id, CancellationToken ct);
}
