using System.Threading.Channels;
using EduCore.Dtos.Pagination;
using EduCore.Exceptions;
using EduCore.Security;
using EduCore.Services;
using EduCore.Services.Interfaces;
using Microsoft.AspNetCore.Http.Features;

namespace EduCore.Endpoints;

public static class NotificationEndpoints
{
    // nginx drops a proxied response that is silent for 60 s; a comment line well inside that keeps
    // the stream open. Also how often a deactivated account's stream is noticed and closed.
    private static readonly TimeSpan PingInterval = TimeSpan.FromSeconds(25);

    public static void MapNotificationEndpoints(this IEndpointRouteBuilder api)
    {
        var group = api.MapGroup("/notifications")
            .RequireAuthorization(Policies.AnySchoolUser)
            .WithTags("Notifications");

        group.MapGet("/", async (bool? unreadOnly, int? page, int? pageSize, INotificationService service, CancellationToken ct) =>
            TypedResults.Ok(await service.GetMineAsync(
                unreadOnly ?? false, new PaginationQueryDto(page ?? 1, pageSize ?? 20), ct)));

        // The authoritative count on demand. The bell does not poll this; it is pushed by /stream.
        group.MapGet("/unread-count", async (INotificationService service, CancellationToken ct) =>
            TypedResults.Ok(new { count = await service.GetUnreadCountAsync(ct) }));

        group.MapGet("/stream", StreamUnreadCountAsync);

        group.MapPost("/{id:guid}/read", async (Guid id, INotificationService service, CancellationToken ct) =>
        {
            await service.MarkReadAsync(id, ct);
            return TypedResults.NoContent();
        });

        group.MapPost("/read-all", async (INotificationService service, CancellationToken ct) =>
        {
            await service.MarkAllReadAsync(ct);
            return TypedResults.NoContent();
        });
    }

    // Server-Sent Events carrying the bell's unread count and nothing else: the notifications
    // themselves are fetched on demand. Sends `event: unread` with {"count":N} on connect and again
    // whenever the count may have changed. Ends when the session token expires or the account is
    // deactivated; the browser's EventSource reconnects, and that reconnect is authenticated afresh.
    private static async Task StreamUnreadCountAsync(
        HttpContext http,
        INotificationService service,
        NotificationSignals signals,
        IAccountStatusCache accounts,
        ICurrentUser currentUser,
        CancellationToken ct)
    {
        var role = currentUser.Role ?? throw new ForbiddenException("You have no role.");

        // The token was checked once, when the stream opened; the stream must not outlive it.
        using var session = CancellationTokenSource.CreateLinkedTokenSource(ct);
        if (long.TryParse(http.User.FindFirst("exp")?.Value, out var exp))
            session.CancelAfter(TimeSpan.FromSeconds(Math.Max(0, exp - DateTimeOffset.UtcNow.ToUnixTimeSeconds())));

        http.Response.ContentType = "text/event-stream";
        http.Response.Headers.CacheControl = "no-cache";
        http.Response.Headers["X-Accel-Buffering"] = "no"; // nginx must not buffer the stream
        http.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();

        // Subscribed before the first count, so a change landing in between is pushed, not lost.
        var signal = signals.Subscribe(role, currentUser.SchoolIds);
        try
        {
            var changed = true;
            while (true)
            {
                // Before every write, not only on quiet pings: a steady run of changes must not keep
                // a deactivated account's stream alive. The status is cached, so this is cheap.
                var status = await accounts.GetAsync(currentUser.UserId, session.Token);
                if (!status.Admits(http.User)) return;

                if (changed)
                {
                    var count = await service.GetUnreadCountAsync(session.Token);
                    await WriteAsync(http, $"event: unread\ndata: {{\"count\":{count}}}\n\n", session.Token);
                }
                else
                {
                    await WriteAsync(http, ": ping\n\n", session.Token);
                }

                changed = await WaitForChangeAsync(signal.Reader, session.Token);
            }
        }
        catch (OperationCanceledException)
        {
            // The browser went away or the token expired.
        }
        finally
        {
            signals.Unsubscribe(signal);
        }
    }

    // True when the count may have changed, false when the ping interval passed quietly.
    private static async Task<bool> WaitForChangeAsync(
        ChannelReader<bool> reader, CancellationToken ct)
    {
        using var wait = CancellationTokenSource.CreateLinkedTokenSource(ct);
        wait.CancelAfter(PingInterval);
        try
        {
            await reader.ReadAsync(wait.Token);
            return true;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return false;
        }
    }

    private static async Task WriteAsync(HttpContext http, string text, CancellationToken ct)
    {
        await http.Response.WriteAsync(text, ct);
        await http.Response.Body.FlushAsync(ct);
    }
}
