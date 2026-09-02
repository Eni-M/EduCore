using EduCore.Dtos.Account;

namespace EduCore.Dtos.Guardian;

public record GuardianListDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    IReadOnlyList<GuardianChildDto> Children,
    AccountDto? Account);
