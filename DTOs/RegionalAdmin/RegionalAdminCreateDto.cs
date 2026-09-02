namespace EduCore.Dtos.RegionalAdmin;

public record RegionalAdminCreateDto(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    Guid CityId);
