using EduCore.Dtos.Account;

namespace EduCore.Dtos.StateAdmin;

public record StateAdminListDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    Guid StateId,
    string StateName,
    AccountDto? Account);
