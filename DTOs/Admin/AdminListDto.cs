using EduCore.Dtos.Account;

namespace EduCore.Dtos.Admin;

// /api/admins is SuperAdmin-only throughout, so Account is always populated here.
// It is still nullable, so that one shape means one thing everywhere it appears.
public record AdminListDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    Guid SchoolId,
    string SchoolName,
    AccountDto? Account);
