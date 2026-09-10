using EduCore.Data;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Repositories;

public class MessagingRepository(AppDbContext context)
    : IMessagingRepository
{
    public async Task AddMessageAsync(Message message)
    {
        await context.Messages.AddAsync(message);
    }

    public async Task<Message?> GetMessageAsync(
        Guid messageId,
        CancellationToken ct)
    {
        return await context.Messages
            .FirstOrDefaultAsync(m => m.Id == messageId, ct);
    }

    public async Task MarkConversationAsReadAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken ct)
    {
        await context.Messages
            .Where(m =>
                m.ConversationId == conversationId &&
                m.SenderUserId != userId &&
                m.ReadAt == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    m => m.ReadAt,
                    DateTime.UtcNow),
                ct);
    }
}