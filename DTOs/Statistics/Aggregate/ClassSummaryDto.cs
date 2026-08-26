namespace EduCore.Dtos.Statistics.Aggregate;

public record ClassSummaryDto(
    Guid ClassId,
    string ClassName,
    int Grade,
    int Students,
    Measure AttendanceRate,
    Measure AverageFinalMark,
    Measure PassRate,
    int AtRiskStudents);
