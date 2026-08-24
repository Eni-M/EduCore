namespace EduCore.Dtos.Student;

public record StudentEditDto(
    string FirstName,
    string LastName,
    string Email,
    DateOnly DateOfBirth,
    string PersonalNumber);
