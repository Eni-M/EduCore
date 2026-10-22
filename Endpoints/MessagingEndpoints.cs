using EduCore.Dtos.Messaging;
using EduCore.Security;
using EduCore.Services.Interfaces;

namespace EduCore.Endpoints;

public static class MessagingEndpoints
{
    public static void MapMessagingEndpoints(
        this IEndpointRouteBuilder api)
    {
        var group = api.MapGroup("/messages")
            .WithTags("Messaging");

        group.MapGet("/conversations",
                async (
                    IMessagingService service,
                    CancellationToken ct) =>
                    TypedResults.Ok(
                        await service.GetMyConversationsAsync(ct)))
            .RequireAuthorization(
                Policies.MessagingUser);

        group.MapGet("/conversations/{conversationId:guid}",
                async (
                    Guid conversationId,
                    IMessagingService service,
                    CancellationToken ct) =>
                    TypedResults.Ok(
                        await service.GetConversationAsync(
                            conversationId,
                            ct)))
            .RequireAuthorization(
                Policies.MessagingUser);

        group.MapPost("/conversations",
                async (
                    StartConversationDto dto,
                    IMessagingService service,
                    CancellationToken ct) =>
                    TypedResults.Ok(
                        await service.StartConversationAsync(
                            dto,
                            ct)))
            .RequireAuthorization(
                Policies.MessagingUser);

        group.MapPost("/conversations/{conversationId:guid}/messages",
                async (
                    Guid conversationId,
                    SendMessageDto dto,
                    IMessagingService service,
                    CancellationToken ct) =>
                    TypedResults.Ok(
                        await service.SendMessageAsync(
                            conversationId,
                            dto,
                            ct)))
            .RequireAuthorization(
                Policies.MessagingUser);

        group.MapPost("/conversations/{conversationId:guid}/read",
                async (
                    Guid conversationId,
                    IMessagingService service,
                    CancellationToken ct) =>
                {
                    await service.MarkAsReadAsync(
                        conversationId,
                        ct);

                    return TypedResults.NoContent();
                })
            .RequireAuthorization(
                Policies.MessagingUser);
    }
}