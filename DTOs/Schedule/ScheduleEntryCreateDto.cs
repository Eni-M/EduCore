namespace EduCore.Dtos.Schedule;

// Teacher is derived from the active ClassSubjectTeacher for (ClassId, SubjectId),
// so the assignment stays the single source of truth for who teaches what.
public record ScheduleEntryCreateDto(
    Guid ClassId,
    Guid SubjectId,
    Guid ShiftPeriodId,
    DayOfWeek DayOfWeek);
