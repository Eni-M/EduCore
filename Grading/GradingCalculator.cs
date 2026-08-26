using Microsoft.Extensions.Options;

namespace EduCore.Grading;

// Every grading rule in the system, in one class.
//
// The weighting also exists as a query in MarkRepository, because a method call cannot be
// translated to SQL and statistics must aggregate in the database. GradingRuleAgreementTests
// runs that query and checks it against this class for every pair of marks, so the two cannot
// drift without a test failing.
public class GradingCalculator(IOptions<GradingOptions> options) : IGradingCalculator
{
    private readonly GradingOptions _options = options.Value;

    // Unrounded. Averages use this: averaging already-rounded grades biases the result.
    public double? WeightedValue(int? first, int? second) =>
        first.HasValue && second.HasValue
            ? first.Value * _options.FirstMarkWeight + second.Value * _options.SecondMarkWeight
            : null;

    // Null until both marks exist, so a half-graded period reads as incomplete, not as a low
    // grade.
    public int? FinalMark(int? first, int? second)
    {
        var weighted = WeightedValue(first, second);

        return weighted is null
            ? null
            : (int)Math.Round(weighted.Value, MidpointRounding.AwayFromZero);
    }

    // Mean of the periods that have a final mark; the rest are skipped, not counted as zero.
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

    // A whole student, not one mark: someone scraping through several subjects is at risk even
    // when no single final mark is exactly the at-risk grade.
    public bool IsStudentAtRisk(double averageOfUnroundedMarks) =>
        averageOfUnroundedMarks < AtRiskUpperValue;
}
