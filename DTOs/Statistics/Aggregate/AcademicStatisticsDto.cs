namespace EduCore.Dtos.Statistics.Aggregate;

// Incomplete counts marks with only one of the two components entered. They are excluded from
// the averages rather than counted low -- otherwise a mid-term view shows every class failing.
public record AcademicStatisticsDto(
    int GradedMarks,
    int IncompleteMarks,
    Measure AverageFinalMark,
    Measure PassRate,
    Measure FailRate,
    Measure AtRiskRate);
