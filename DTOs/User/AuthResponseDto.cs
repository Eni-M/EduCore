namespace EduCore.Dtos.User;

public record AuthResponseDto(
    string Token,
    DateTime ExpiresAt,
    Guid UserId,
    Guid? SchoolId,
    Guid? TeacherId,
    Guid? StudentId,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    bool MustChangePassword);
