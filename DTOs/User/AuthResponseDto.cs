namespace EduCore.Dtos.User;

public record AuthResponseDto(
    DateTime ExpiresAt,
    Guid UserId,
    Guid? SchoolId,
    Guid? TeacherId,
    Guid? StudentId,
    Guid? CityId,
    Guid? StateId,
    Guid? GuardianId,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    bool MustChangePassword);
