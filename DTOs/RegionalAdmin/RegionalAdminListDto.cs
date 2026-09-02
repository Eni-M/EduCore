using EduCore.Dtos.Account;

namespace EduCore.Dtos.RegionalAdmin;

public record RegionalAdminListDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    Guid CityId,
    string CityName,
    string StateName,
    AccountDto? Account);
