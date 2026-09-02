namespace EduCore.Dtos.Guardian;

public record GuardianContactDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber);
