namespace EduCore.Dtos.Admin;

public record AdminEditDto(
    string FirstName,
    string LastName,
    string Email,
    Guid SchoolId);
