namespace EduCore.Dtos.Assignment;

public record AssignmentDetailsDto(
    Guid Id,
    Guid ClassId,
    string ClassName,
    Guid SubjectId,
    string SubjectName,
    Guid TeacherId,
    string TeacherName,
    string Title,
    string? Description,
    DateTime DueDate,
    string? FileName,
    string? ContentType,
    long? SizeBytes,
    DateTime CreatedAt,
    DateTime UpdatedAt);