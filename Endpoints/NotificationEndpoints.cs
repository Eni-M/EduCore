using EduCore.Security;
using EduCore.Services.Interfaces;

namespace EduCore.Endpoints;

public static class NotificationEndpoints
{
    public static void MapNotificationEndpoints(this IEndpointRouteBuilder api)
    {
        var group = api.MapGroup("/notifications")
            .RequireAuthorization(Policies.AnySchoolUser)
            .WithTags("Notifications");

        group.MapGet("/", async (bool? unreadOnly, INotificationService service, CancellationToken ct) =>
            Results.Ok(await service.GetMineAsync(unreadOnly ?? false, ct)));

        group.MapGet("/unread-count", async (INotificationService service, CancellationToken ct) =>
            Results.Ok(new { count = await service.GetUnreadCountAsync(ct) }));

        group.MapPost("/{id:guid}/read", async (Guid id, INotificationService service, CancellationToken ct) =>
        {
            await service.MarkReadAsync(id, ct);
            return Results.NoContent();
        });
    }
}
