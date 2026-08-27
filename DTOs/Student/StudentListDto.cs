using EduCore.Dtos.Account;

namespace EduCore.Dtos.Student;

// PersonalNumber is a national-ID-equivalent -- kept out of the list view (bulk exposure)
// and only returned in the single-record detail view.
//
// Account is null unless the caller may manage sign-ins (Admin, SuperAdmin). The
// paged roll at /api/students is administrator-only and always carries it; the
// class roster at /api/classes/{id}/students is also reachable by a teacher, who
// gets the same rows with the block stripped.
public record StudentListDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    DateOnly DateOfBirth,
    bool IsGraduated,
    AccountDto? Account);
