namespace EduCore.Dtos.Statistics.Aggregate;

public record AcademicStatisticsDto(
    int GradedMarks,
    int IncompleteMarks,
    Measure AverageFinalMark,
    Measure PassRate,
    Measure FailRate,
    Measure AtRiskRate);
