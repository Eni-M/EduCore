namespace EduCore.Dtos.Attendance;

// Correcting a mistaken mark, or adding a behavior comment after the fact.
//
// IsExcused is only meaningful on an absence. That invariant is enforced by Attendance.Record,
// which drops an excuse for a present student rather than storing a contradiction -- there is
// deliberately no check constraint and no validator rule, so the rule lives in exactly one
// place and cannot be forgotten at a call site.
public record AttendanceEditDto(bool IsPresent, bool IsExcused, string? Comment, int? BehaviorMark);
