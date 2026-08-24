namespace EduCore.Dtos.Student;

// PersonalNumber is a national-ID-equivalent -- kept out of the list view (bulk exposure)
// and only returned in the single-record detail view.
public record StudentListDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    DateOnly DateOfBirth,
    bool IsGraduated);
