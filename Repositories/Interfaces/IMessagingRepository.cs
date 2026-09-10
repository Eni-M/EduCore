using EduCore.Models;

namespace EduCore.Repositories.Interfaces;

public interface IMessagingRepository
{
    Task AddMessageAsync(Message message);

    Task<Message?> GetMessageAsync(
        Guid messageId,
        CancellationToken ct);

    Task MarkConversationAsReadAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken ct);
}