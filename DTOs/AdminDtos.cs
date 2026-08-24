namespace EduCore.Dtos;

public record AdminListDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    Guid SchoolId,
    string SchoolName);

public record AdminCreateDto(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    Guid SchoolId);

public record AdminEditDto(
    string FirstName,
    string LastName,
    string Email,
    Guid SchoolId);
