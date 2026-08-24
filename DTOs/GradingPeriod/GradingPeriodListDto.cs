namespace EduCore.Dtos.GradingPeriod;

public record GradingPeriodListDto(
    Guid Id,
    Guid AcademicYearId,
    string AcademicYearName,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate);
