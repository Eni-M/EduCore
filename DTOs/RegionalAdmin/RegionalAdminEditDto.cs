namespace EduCore.Dtos.RegionalAdmin;

public record RegionalAdminEditDto(
    string FirstName,
    string LastName,
    string Email,
    Guid CityId);
