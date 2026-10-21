using EduCore.Dtos.Notification;
using EduCore.Dtos.Pagination;
using EduCore.Enums;

namespace EduCore.Services.Interfaces;

public interface INotificationService
{
    // Raise a notification for everyone of a role within a school. Saves immediately and wakes
    // that role's open bell streams.
    Task NotifyRoleAsync(Guid schoolId, UserRole role, string message, string? link, CancellationToken ct);

    // The current user's notifications (matched to their role, within their school scope).
    // Fetched on demand only -- when the bell opens or the notifications page loads; the stream
    // carries nothing but the unread count.
    Task<PagedResultDto<NotificationDto>> GetMineAsync(bool unreadOnly, PaginationQueryDto pagination, CancellationToken ct);
    Task<int> GetUnreadCountAsync(CancellationToken ct);
    Task MarkReadAsync(Guid id, CancellationToken ct);
    Task MarkAllReadAsync(CancellationToken ct);
}
