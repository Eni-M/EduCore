using EduCore.Dtos.Messaging;

public record ConversationDetailsDto(
    Guid Id,
    Guid StudentId,
    string StudentName,
    Guid GuardianId,
    Guid GuardianUserId,
    string GuardianName,
    Guid TeacherId,
    Guid TeacherUserId,
    string TeacherName,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<MessageDto> Messages);