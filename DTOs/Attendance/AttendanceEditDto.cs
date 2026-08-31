namespace EduCore.Dtos.Attendance;

public record AttendanceEditDto(bool IsPresent, bool IsExcused, string? Comment, int? BehaviorMark);
