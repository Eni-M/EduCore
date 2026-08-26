namespace EduCore.Dtos.Statistics.Rows;

// Raw numbers straight out of SQL: no baselines, no suppression, no formatting. Turning these
// into Measures is the service's job, which keeps the queries readable.
public record AcademicTotals(
    int GradedMarks,
    int IncompleteMarks,
    double? AverageFinalMark,
    int Passed,
    int Failed,
    int AtRisk);
