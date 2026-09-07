namespace EduCore.Dtos.Assignment;

public record CreateAssignmentRequest(
    Guid ClassId,
    Guid SubjectId,
    string Title,
    string? Description,
    DateTime DueDate);