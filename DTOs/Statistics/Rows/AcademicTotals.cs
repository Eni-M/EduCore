namespace EduCore.Dtos.Statistics.Rows;

public record AcademicTotals(
    int GradedMarks,
    int IncompleteMarks,
    double? AverageFinalMark,
    int Passed,
    int Failed,
    int AtRisk);
