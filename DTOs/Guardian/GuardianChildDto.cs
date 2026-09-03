using EduCore.Enums;

namespace EduCore.Dtos.Guardian;

public record GuardianChildDto(
    Guid StudentId,
    string FirstName,
    string LastName,
    string SchoolName,
    SchoolType SchoolType);
