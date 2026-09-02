namespace EduCore.Dtos.Guardian;

public record GuardianSearchResultDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string NationalId,
    string PhoneNumber);
