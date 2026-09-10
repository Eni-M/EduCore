using EduCore.Dtos.Messaging;

namespace EduCore.Services.Interfaces;

public interface IMessagingService
{
    Task<IEnumerable<ConversationListDto>> GetMyConversationsAsync(
        CancellationToken ct);

    Task<ConversationDetailsDto> GetConversationAsync(
        Guid conversationId,
        CancellationToken ct);

    Task<ConversationDetailsDto> StartConversationAsync(
        StartConversationDto dto,
        CancellationToken ct);

    Task<ConversationDetailsDto> SendMessageAsync(
        Guid conversationId,
        SendMessageDto dto,
        CancellationToken ct);

    Task MarkAsReadAsync(
        Guid conversationId,
        CancellationToken ct);
}