using EduCore.Dtos.Account;

namespace EduCore.Dtos.Teacher;

public record TeacherListDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    IReadOnlyList<string> Subjects,
    AccountDto? Account);
