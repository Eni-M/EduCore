namespace EduCore.Dtos.Directorate;

public record DirectorateEditDto(
    string FirstName,
    string LastName,
    string Email,
    Guid SchoolId);
