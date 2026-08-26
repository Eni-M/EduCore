namespace EduCore.Dtos.Attendance;

// Correcting a mistaken mark, or adding a behavior comment after the fact. IsExcused is only
// valid on an absence -- a DB check constraint backs this up.
public record AttendanceEditDto(bool IsPresent, bool IsExcused, string? Comment, int? BehaviorMark);
