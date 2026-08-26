using EduCore.Services.Statistics.Interfaces;
using Microsoft.Extensions.Options;

namespace EduCore.Services.Statistics;

// The one place the grading rule lives.
//
// The rule appears in exactly two forms in this codebase: here, and once as a LINQ expression
// in AcademicStatisticsRepository (which has to run in SQL to aggregate). A unit test asserts
// the two agree across every mark combination -- if they ever drift, the database and the
// application would disagree about a student's grade and nothing else would fail.
public class GradingCalculator(IOptions<GradingOptions> options) : IGradingCalculator
{
    private readonly GradingOptions _options = options.Value;

    // The weighted mark before rounding. Averages use this: averaging already-rounded grades
    // throws away precision and biases class averages.
    public double? WeightedValue(int? first, int? second) =>
        first.HasValue && second.HasValue
            ? first.Value * _options.FirstMarkWeight + second.Value * _options.SecondMarkWeight
            : null;

    // The student's actual grade -- an integer on the 1-5 scale. Null until both marks exist,
    // so a half-graded period reads as incomplete rather than as a low grade.
    public int? FinalMark(int? first, int? second)
    {
        var weighted = WeightedValue(first, second);

        return weighted is null
            ? null
            : (int)Math.Round(weighted.Value, MidpointRounding.AwayFromZero);
    }

    // A year's grade for a subject is the mean of its grading periods' final marks. Periods
    // still missing a final mark are skipped rather than counted as zero.
    public int? AnnualMark(IEnumerable<int?> periodFinalMarks)
    {
        var graded = periodFinalMarks.Where(m => m.HasValue).Select(m => m!.Value).ToList();

        return graded.Count == 0
            ? null
            : (int)Math.Round(graded.Average(), MidpointRounding.AwayFromZero);
    }

    public bool IsPass(int finalMark) => finalMark >= _options.PassMark;
    public bool IsFail(int finalMark) => finalMark < _options.PassMark;
    public bool IsAtRisk(int finalMark) => finalMark == _options.AtRiskMark;
}
