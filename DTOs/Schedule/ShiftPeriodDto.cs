namespace EduCore.Dtos.Schedule;

public record ShiftPeriodDto(
    Guid Id,
    Guid SchoolShiftId,
    int PeriodNumber,
    TimeOnly StartTime,
    TimeOnly EndTime);
