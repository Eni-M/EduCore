using EduCore.Enums;
using EduCore.Models;

namespace EduCore.Repositories.Interfaces;

public interface INotificationRepository : IRepository<Notification>
{
    // Newest first, for the given role within the caller's school scope.
    Task<IReadOnlyList<Notification>> GetForRoleAsync(UserRole role, bool unreadOnly, CancellationToken ct);
    Task<int> CountUnreadForRoleAsync(UserRole role, CancellationToken ct);
}
