namespace EduCore.Grading;

// The grading rules of the state the school operates in.
//
// Configuration rather than constants: the 40/60 split is believed right but not certain, and
// changing it must not mean a code change. Validated at startup by GradingOptionsValidator, so
// a bad weighting fails the app on boot instead of quietly skewing every report.
//
// Pure configuration -- values only. Anything derived from these (thresholds, rounding) is a
// rule and lives on GradingCalculator.
public class GradingOptions
{
    public const string SectionName = "Grading";

    // Named for their position, not for what they measure -- what FirstMark and SecondMark
    // represent is the teacher's own decision and is deliberately not recorded.
    public double FirstMarkWeight { get; set; } = 0.40;
    public double SecondMarkWeight { get; set; } = 0.60;

    public int PassMark { get; set; } = 2;
    public int AtRiskMark { get; set; } = 2;
}
