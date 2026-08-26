using Microsoft.Extensions.Options;

namespace EduCore.Grading;

// Every grading rule in the system, in one class.
//
// The weighting itself also exists as a LINQ query in MarkRepository.Value, because a method
// call cannot be translated to SQL and statistics have to aggregate in the database rather
// than load a decade of marks into memory. That query is built from
// MarkRepository.WeightedValueOf, and GradingRuleAgreementTests compiles that very expression
// and checks it against this class for every possible pair of marks -- so the two cannot drift
// without a test failing.
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

    // Rounding is half-away-from-zero, so a rounded mark reaches PassMark exactly when the
    // unrounded value reaches PassMark - 0.5. Comparing the unrounded value keeps aggregation
    // to plain arithmetic the database can run, rather than depending on how ROUND translates.
    public double FirstMarkWeight => _options.FirstMarkWeight;
    public double SecondMarkWeight => _options.SecondMarkWeight;

    public double PassValue => _options.PassMark - 0.5;
    public double AtRiskLowerValue => _options.AtRiskMark - 0.5;
    public double AtRiskUpperValue => _options.AtRiskMark + 0.5;

    public bool IsPass(int finalMark) => finalMark >= _options.PassMark;
    public bool IsFail(int finalMark) => finalMark < _options.PassMark;
    public bool IsAtRisk(int finalMark) => finalMark == _options.AtRiskMark;

    // Judges a whole student rather than one mark: their mean across every subject, unrounded.
    // A student scraping through several subjects is at risk even if no single final mark is
    // exactly the at-risk grade, which is why this is not IsAtRisk applied to an average.
    public bool IsStudentAtRisk(double averageOfUnroundedMarks) =>
        averageOfUnroundedMarks < AtRiskUpperValue;
}
