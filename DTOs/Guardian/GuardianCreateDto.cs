namespace EduCore.Dtos.Guardian;

public record GuardianCreateDto(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string NationalId,
    string PhoneNumber,
    IReadOnlyList<Guid> StudentIds);
