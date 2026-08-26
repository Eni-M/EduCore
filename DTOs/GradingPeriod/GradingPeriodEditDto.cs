namespace EduCore.Dtos.GradingPeriod;

// The only way a grading period ever changes: an academic year always has exactly two, so
// there is no create or delete -- only moving a period's own dates.
public record GradingPeriodEditDto(DateOnly StartDate, DateOnly EndDate);
