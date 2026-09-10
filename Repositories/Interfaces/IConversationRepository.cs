using EduCore.Dtos.Messaging;
using EduCore.Models;

namespace EduCore.Repositories.Interfaces;

public interface IConversationRepository : IRepository<Conversation>
{
    Task<Conversation?> GetByParticipantsAsync(
        Guid studentId,
        Guid guardianId,
        Guid teacherId,
        CancellationToken ct);

    Task<IEnumerable<ConversationListDto>> GetForGuardianAsync(
        Guid guardianId,
        CancellationToken ct);

    Task<IEnumerable<ConversationListDto>> GetForTeacherAsync(
        Guid teacherId,
        CancellationToken ct);

    Task<ConversationDetailsDto?> GetDetailsAsync(
        Guid conversationId,
        CancellationToken ct);

    Task<bool> ExistsAsync(
        Guid studentId,
        Guid guardianId,
        Guid teacherId,
        CancellationToken ct);
}