namespace EduCore.Dtos.Guardian;

public record GuardianCreateDto(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string PhoneNumber,
    IReadOnlyList<Guid> StudentIds);
