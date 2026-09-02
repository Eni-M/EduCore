namespace EduCore.Dtos.Schedule;

public record ShiftPeriodWriteDto(int PeriodNumber, TimeOnly StartTime, TimeOnly EndTime);
