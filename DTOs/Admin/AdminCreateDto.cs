namespace EduCore.Dtos.Admin;

public record AdminCreateDto(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    Guid SchoolId);
