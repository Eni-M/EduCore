using EduCore.Dtos.Notification;
using EduCore.Dtos.Pagination;
using EduCore.Enums;
using EduCore.Models;

namespace EduCore.Repositories.Interfaces;

public interface INotificationRepository : IRepository<Notification>
{
    // Newest first, for the given role within the caller's school scope, with this user's read time.
    Task<PagedResultDto<NotificationDto>> GetPagedForUserAsync(
        UserRole role, Guid userId, bool unreadOnly, PaginationQueryDto pagination, CancellationToken ct);
    Task<int> CountUnreadAsync(UserRole role, Guid userId, CancellationToken ct);
    Task<int> MarkAllReadAsync(UserRole role, Guid userId, DateTime readAt, CancellationToken ct);
    Task<bool> IsReadByAsync(Guid notificationId, Guid userId, CancellationToken ct);
    void AddRead(NotificationRead read);
}
