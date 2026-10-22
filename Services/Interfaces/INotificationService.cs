using EduCore.Dtos.Notification;
using EduCore.Dtos.Pagination;
using EduCore.Enums;

namespace EduCore.Services.Interfaces;

public interface INotificationService
{
    // Adds a notification for everyone of a role within a school to the caller's unit of work. It
    // is saved by the caller's own SaveChanges, together with the change it reports, so the two
    // commit or fail as one; the save wakes that role's bell streams.
    Task StageForRoleAsync(Guid schoolId, UserRole role, string message, string? link, CancellationToken ct);

    // The current user's notifications (matched to their role, within their school scope).
    // Fetched on demand only -- when the bell opens or the notifications page loads; the stream
    // carries nothing but the unread count.
    Task<PagedResultDto<NotificationDto>> GetMineAsync(bool unreadOnly, PaginationQueryDto pagination, CancellationToken ct);
    Task<int> GetUnreadCountAsync(CancellationToken ct);
    Task MarkReadAsync(Guid id, CancellationToken ct);
    Task MarkAllReadAsync(CancellationToken ct);
}
