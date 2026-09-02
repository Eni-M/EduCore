using EduCore.Dtos.Attendance;
using EduCore.Dtos.Guardian;
using EduCore.Dtos.Mark;
using EduCore.Enums;

namespace EduCore.Dtos.Student;

public record StudentProfileDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    DateOnly DateOfBirth,
    string PersonalNumber,
    DateOnly? GraduationDate,
    Gender? Gender,
    IReadOnlyList<GuardianContactDto> Guardians,
    Guid? CurrentClassId,
    string? CurrentClassName,
    int? CurrentGrade,
    string? CurrentHomeroomTeacherName,
    IReadOnlyList<AttendanceListDto> Attendances,
    IReadOnlyList<MarkListDto> Marks);
