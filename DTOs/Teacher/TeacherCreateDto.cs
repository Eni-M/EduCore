namespace EduCore.Dtos.Teacher;

public record TeacherCreateDto(
    string FirstName,
    string LastName,
    string Email,
    string Password);
