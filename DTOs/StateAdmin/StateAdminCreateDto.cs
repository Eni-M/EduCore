namespace EduCore.Dtos.StateAdmin;

public record StateAdminCreateDto(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    Guid StateId);
