namespace EduCore.Dtos.Student;

// Creates the underlying User together with the Student profile in one call.
public record StudentCreateDto(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    DateOnly DateOfBirth,
    string PersonalNumber);
