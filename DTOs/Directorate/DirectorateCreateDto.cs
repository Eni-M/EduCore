namespace EduCore.Dtos.Directorate;

public record DirectorateCreateDto(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    Guid SchoolId);
