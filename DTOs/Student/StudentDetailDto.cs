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
    // Resolved for display. CityId is what gets written back on an edit; the two names are
    // read-only and save the caller a second lookup just to render an address.
    Guid? CityId,
    string? CityName,
    string? StateName,
    IReadOnlyList<ParentListDto> Parents);
