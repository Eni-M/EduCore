namespace EduCore.Dtos.StateAdmin;

public record StateAdminEditDto(
    string FirstName,
    string LastName,
    string Email,
    Guid StateId);
