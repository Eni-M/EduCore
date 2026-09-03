using EduCore.Enums;

namespace EduCore.Dtos.Student;

public record StudentEditDto(
    string FirstName,
    string LastName,
    string Email,
    DateOnly DateOfBirth,
    string PersonalNumber,
    Gender Gender,
    Guid? CountryId,
    Guid? OriginCityId,
    string? Address);
