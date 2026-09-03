using EduCore.Enums;

namespace EduCore.Dtos.Student;

public record StudentCreateDto(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    DateOnly DateOfBirth,
    string PersonalNumber,
    Gender Gender,
    Guid? CountryId,
    Guid? OriginCityId,
    string? Address);
