namespace EduCore.Dtos.Messaging;

public record ConversationListDto(
    Guid Id,
    Guid StudentId,
    string StudentName,
    Guid OtherUserId,
    string OtherUserName,
    DateTime UpdatedAt,
    int UnreadCount);