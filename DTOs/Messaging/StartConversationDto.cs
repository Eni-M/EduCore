namespace EduCore.Dtos.Messaging;

public record StartConversationDto(
    Guid StudentId,
    Guid GuardianId,
    Guid TeacherId);