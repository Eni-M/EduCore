using EduCore.Dtos.Pagination;
using EduCore.Enums;
using EduCore.Models;

namespace EduCore.Repositories.Interfaces;

public interface INotificationRepository : IRepository<Notification>
{
    // Newest first, for the given role within the caller's school scope.
    Task<PagedResultDto<Notification>> GetPagedForRoleAsync(
        UserRole role, bool unreadOnly, PaginationQueryDto pagination, CancellationToken ct);
    Task<int> CountUnreadForRoleAsync(UserRole role, CancellationToken ct);
    Task<int> MarkAllReadForRoleAsync(UserRole role, DateTime readAt, CancellationToken ct);
}
