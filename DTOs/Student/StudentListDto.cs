using EduCore.Dtos.Account;

namespace EduCore.Dtos.Student;

public record StudentListDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    DateOnly DateOfBirth,
    bool IsGraduated,
    AccountDto? Account);
