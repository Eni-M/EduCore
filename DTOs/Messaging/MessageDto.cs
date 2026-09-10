namespace EduCore.Dtos.Messaging;

public record MessageDto(
    Guid Id,
    Guid ConversationId,
    Guid SenderUserId,
    string SenderName,
    string Content,
    DateTime SentAt,
    DateTime? ReadAt);