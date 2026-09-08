namespace EduCore.Dtos.Assignment;

public record AssignmentCalendarDto(
    Guid Id,
    string Title,
    string? Description,
    Guid ClassId,
    string ClassName,
    DateTimeOffset DueDate,
    Guid TeacherId,
    string TeacherName);