using EduCore.Dtos.Notification;
using EduCore.Dtos.Pagination;
using EduCore.Enums;
using EduCore.Exceptions;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using EduCore.Security;
using EduCore.Services.Interfaces;

namespace EduCore.Services;

public class NotificationService(
    INotificationRepository notifications,
    ICurrentUser currentUser,
    NotificationSignals signals) : INotificationService
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
        signals.Changed(schoolId, role);
    }

    public async Task<PagedResultDto<NotificationDto>> GetMineAsync(
        bool unreadOnly, PaginationQueryDto pagination, CancellationToken ct)
    {
        var page = await notifications.GetPagedForRoleAsync(RequireRole(), unreadOnly, pagination, ct);

        return new PagedResultDto<NotificationDto>(
            page.Items.Select(n => new NotificationDto(n.Id, n.Message, n.Link, n.CreatedAt, n.ReadAt)).ToList(),
            page.Page, page.PageSize, page.TotalCount);
    }

    public async Task<int> GetUnreadCountAsync(CancellationToken ct) =>
        await notifications.CountUnreadForRoleAsync(RequireRole(), ct);

    public async Task MarkReadAsync(Guid id, CancellationToken ct)
    {
        // The school filter keeps this to notifications in the caller's own school.
        var notification = await notifications.GetByIdAsync(id, ct)
                           ?? throw new NotFoundException("Notification", id);

        if (notification.ReadAt is not null) return;

        notification.ReadAt = DateTime.UtcNow;
        notifications.Update(notification);
        await notifications.SaveChangesAsync(ct);
        signals.Changed(notification.SchoolId, notification.RecipientRole);
    }

    public async Task MarkAllReadAsync(CancellationToken ct)
    {
        var role = RequireRole();
        if (await notifications.MarkAllReadForRoleAsync(role, DateTime.UtcNow, ct) == 0) return;

        foreach (var schoolId in currentUser.SchoolIds)
            signals.Changed(schoolId, role);
    }

    private UserRole RequireRole() => currentUser.Role ?? throw new ForbiddenException("You have no role.");
}
