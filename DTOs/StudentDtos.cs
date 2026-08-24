namespace EduCore.Dtos;

// PersonalNumber is a national-ID-equivalent -- kept out of the list view (bulk exposure)
// and only returned in the single-record detail view.
public record StudentListDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    DateOnly DateOfBirth,
    bool IsGraduated);

public record StudentDetailDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    DateOnly DateOfBirth,
    string PersonalNumber,
    DateOnly? GraduationDate,
    IReadOnlyList<ParentListDto> Parents);

// Creates the underlying User together with the Student profile in one call.
public record StudentCreateDto(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    DateOnly DateOfBirth,
    string PersonalNumber);

public record StudentEditDto(
    string FirstName,
    string LastName,
    string Email,
    DateOnly DateOfBirth,
    string PersonalNumber);

// Graduation is a deliberate, admin-supplied-date action (Student.Graduate), not a plain field edit.
public record StudentGraduateDto(DateOnly GraduationDate);
