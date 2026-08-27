using EduCore.Enums;

namespace EduCore.Dtos.Student;

// No guardian fields here: guardians are their own records with their own lifecycle, edited
// through /api/parents. Only the city, which is part of the student's own identity, is here.
public record StudentEditDto(
    string FirstName,
    string LastName,
    string Email,
    DateOnly DateOfBirth,
    string PersonalNumber,
    Gender Gender,
    Guid? CityId);
