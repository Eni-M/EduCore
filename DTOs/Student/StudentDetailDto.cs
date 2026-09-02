using EduCore.Dtos.Guardian;
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
    Guid? CityId,
    string? CityName,
    string? StateName,
    IReadOnlyList<GuardianContactDto> Guardians);
