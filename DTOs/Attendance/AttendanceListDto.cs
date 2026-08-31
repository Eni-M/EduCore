namespace EduCore.Dtos.Attendance;

public record AttendanceListDto(
    Guid Id,
    Guid DiaryEntryId,
    DateOnly Date,
    string SubjectName,
    string ClassName,
    Guid EnrollmentId,
    string StudentName,
    bool IsPresent,
    bool IsExcused,
    string? Comment,
    int? BehaviorMark);
