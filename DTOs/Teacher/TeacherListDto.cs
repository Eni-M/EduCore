namespace EduCore.Dtos.Teacher;

public record TeacherListDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email);
