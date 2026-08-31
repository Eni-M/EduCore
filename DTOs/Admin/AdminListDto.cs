using EduCore.Dtos.Account;

namespace EduCore.Dtos.Admin;

public record AdminListDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    Guid SchoolId,
    string SchoolName,
    AccountDto? Account);
