using EduCore.Dtos.Parent;
using EduCore.Enums;

namespace EduCore.Dtos.Student;

public record StudentDetailDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    DateOnly DateOfBirth,
    string PersonalNumber,
    DateOnly? GraduationDate,
    Gender? Gender,
    IReadOnlyList<ParentListDto> Parents);
