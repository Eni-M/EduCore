using EduCore.Dtos.Attendance;
using EduCore.Dtos.Mark;
using EduCore.Dtos.Parent;

namespace EduCore.Dtos.Student;

// The full "everything about this student" view: identity, guardians, current placement,
// and their complete attendance/behavior/academic history in one call.
public record StudentProfileDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    DateOnly DateOfBirth,
    string PersonalNumber,
    DateOnly? GraduationDate,
    IReadOnlyList<ParentListDto> Parents,
    Guid? CurrentClassId,
    string? CurrentClassName,
    int? CurrentGrade,
    IReadOnlyList<AttendanceListDto> Attendances,
    IReadOnlyList<MarkListDto> Marks);
