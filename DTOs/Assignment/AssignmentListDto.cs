namespace EduCore.Dtos.Assignment;

public record AssignmentListDto(
    Guid Id,
    Guid ClassId,
    string ClassName,
    Guid SubjectId,
    string SubjectName,
    Guid TeacherId,
    string TeacherName,
    string Title,
    DateTimeOffset DueDate,
    bool HasAttachment,
    DateTimeOffset CreatedAt);
