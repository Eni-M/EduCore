namespace EduCore.Dtos;

public record TeacherListDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email);

public record TeacherDetailDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    IReadOnlyList<string> Subjects);

public record TeacherCreateDto(
    string FirstName,
    string LastName,
    string Email,
    string Password);

public record TeacherEditDto(
    string FirstName,
    string LastName,
    string Email);
