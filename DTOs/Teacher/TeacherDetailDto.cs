namespace EduCore.Dtos.Teacher;

public record TeacherDetailDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    IReadOnlyList<string> Subjects);
