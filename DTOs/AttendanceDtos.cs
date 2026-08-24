namespace EduCore.Dtos;

public record AttendanceListDto(
    Guid Id,
    Guid EnrollmentId,
    string StudentName,
    string ClassName,
    DateTime Date,
    bool IsPresent);

public record AttendanceCreateDto(Guid EnrollmentId, DateTime Date, bool IsPresent);
