namespace EduCore.Dtos.Attendance;

public record AttendanceCreateDto(
    Guid DiaryEntryId,
    Guid EnrollmentId,
    bool IsPresent,
    string? Comment,
    int? BehaviorMark);
