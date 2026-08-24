namespace EduCore.Dtos.GradingPeriod;

public record GradingPeriodCreateDto(Guid AcademicYearId, string Name, DateOnly StartDate, DateOnly EndDate);
