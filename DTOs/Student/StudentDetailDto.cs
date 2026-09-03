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
    Guid? CountryId,
    string? CountryName,
    Guid? OriginCityId,
    string? OriginCityName,
    string? Address,
    IReadOnlyList<GuardianContactDto> Guardians);
