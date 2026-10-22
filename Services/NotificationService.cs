using EduCore.Dtos.Notification;
using EduCore.Dtos.Pagination;
using EduCore.Enums;
using EduCore.Exceptions;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using EduCore.Security;
using EduCore.Services.Interfaces;

namespace EduCore.Services;

// Bell streams are woken by NotificationSignalInterceptor whenever notification rows are saved,
// so nothing here signals by hand.
public class NotificationService(
    INotificationRepository notifications,
    ICurrentUser currentUser) : INotificationService
{
    public async Task StageForRoleAsync(
        Guid schoolId, UserRole role, string message, string? link, CancellationToken ct) =>
        await notifications.AddAsync(new Notification
        {
            SchoolId = schoolId,
            RecipientRole = role,
            Message = message,
            Link = link,
            CreatedAt = DateTime.UtcNow,
        });

    public async Task<PagedResultDto<NotificationDto>> GetMineAsync(
        bool unreadOnly, PaginationQueryDto pagination, CancellationToken ct) =>
        await notifications.GetPagedForUserAsync(RequireRole(), currentUser.UserId, unreadOnly, pagination, ct);

    public async Task<int> GetUnreadCountAsync(CancellationToken ct) =>
        await notifications.CountUnreadAsync(RequireRole(), currentUser.UserId, ct);

    public async Task MarkReadAsync(Guid id, CancellationToken ct)
    {
        // The school filter keeps this to notifications in the caller's own school, and the role
        // check to the caller's own bell.
        var notification = await notifications.GetByIdAsync(id, ct);
        if (notification is null || notification.RecipientRole != RequireRole())
            throw new NotFoundException("Notification", id);

        if (await notifications.IsReadByAsync(id, currentUser.UserId, ct))
            return;

        notifications.AddRead(new NotificationRead
        {
            NotificationId = id,
            UserId = currentUser.UserId,
            ReadAt = DateTime.UtcNow,
            SchoolId = notification.SchoolId,
            RecipientRole = notification.RecipientRole,
        });
        await notifications.SaveChangesAsync(ct);
    }

    public async Task MarkAllReadAsync(CancellationToken ct) =>
        await notifications.MarkAllReadAsync(RequireRole(), currentUser.UserId, DateTime.UtcNow, ct);

    private UserRole RequireRole() => currentUser.Role ?? throw new ForbiddenException("You have no role.");
}
