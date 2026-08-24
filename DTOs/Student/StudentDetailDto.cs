using EduCore.Dtos.Parent;

namespace EduCore.Dtos.Student;

public record StudentDetailDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    DateOnly DateOfBirth,
    string PersonalNumber,
    DateOnly? GraduationDate,
    IReadOnlyList<ParentListDto> Parents);
