namespace EduCore.Dtos.Attendance;

// General-purpose: used both nested under a DiaryEntryDetailDto (Date/Subject/Class are
// redundant there, but harmless) and standalone in a student's attendance history (where
// they're exactly what's needed).
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
