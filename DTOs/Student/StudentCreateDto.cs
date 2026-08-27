using EduCore.Enums;

namespace EduCore.Dtos.Student;

// Creates the underlying User together with the Student profile in one call.
//
// CityId is optional -- a school may not know where a pupil lives on the day they enrol --
// and the state is reached through the city rather than stored a second time.
//
// GuardianName/GuardianPhone are the one guardian a school almost always has at enrolment.
// Recording them here rather than forcing a second call to /api/parents means a student is
// never left with no contact number because the form was abandoned halfway. Further
// guardians are added afterwards through /api/parents as before.
public record StudentCreateDto(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    DateOnly DateOfBirth,
    string PersonalNumber,
    Gender Gender,
    Guid? CityId,
    string? GuardianName,
    string? GuardianPhone);
