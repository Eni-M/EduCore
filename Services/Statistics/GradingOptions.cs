namespace EduCore.Services.Statistics;

// The grading rules of the state the school operates in. Configuration rather than constants:
// the 40/60 split is believed right but not certain, and changing it must not mean a code
// change. Validated at startup (see Program.cs) so a bad weighting fails the app on boot
// instead of quietly skewing every report.
public class GradingOptions
{
    public const string SectionName = "Grading";

    // Named for their position, not for what they measure -- what FirstMark and SecondMark
    // represent is the teacher's own decision and is deliberately not recorded.
    public double FirstMarkWeight { get; set; } = 0.40;
    public double SecondMarkWeight { get; set; } = 0.60;

    public int PassMark { get; set; } = 2;
    public int AtRiskMark { get; set; } = 2;

    // Aggregates computed from fewer rows than this are suppressed rather than returned --
    // small buckets identify individual students.
    public int MinimumCellSize { get; set; } = 5;

    // Final marks are rounded half-away-from-zero, so a rounded mark reaches PassMark exactly
    // when the unrounded value reaches PassMark - 0.5. Queries compare against these instead
    // of rounding in SQL, which keeps the comparison to plain arithmetic the database can run.
    public double PassValue => PassMark - 0.5;
    public double AtRiskLowerValue => AtRiskMark - 0.5;
    public double AtRiskUpperValue => AtRiskMark + 0.5;
}
