using EduCore.Dtos.Account;

namespace EduCore.Dtos.Teacher;

// Account is null unless the caller may manage sign-ins. /api/teachers is open to
// teachers as well as administrators, and a colleague's user id and deactivation
// date are an administrator's business, not a colleague's.
public record TeacherListDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    IReadOnlyList<string> Subjects,
    AccountDto? Account);
