namespace EduCore.Dtos.Schedule;

public record ScheduleEntryDto(
    Guid Id,
    Guid ClassId,
    string ClassName,
    Guid SubjectId,
    string SubjectName,
    Guid TeacherId,
    string TeacherName,
    Guid ShiftPeriodId,
    int PeriodNumber,
    TimeOnly StartTime,
    TimeOnly EndTime,
    DayOfWeek DayOfWeek);
