using EduCore.Dtos.Account;

namespace EduCore.Dtos.Directorate;

public record DirectorateListDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    Guid SchoolId,
    string SchoolName,
    AccountDto? Account);
