namespace EduCore.Dtos.Attendance;

// Correcting a mistaken mark, or adding a behavior comment after the fact.
public record AttendanceEditDto(bool IsPresent, string? Comment, int? BehaviorMark);
