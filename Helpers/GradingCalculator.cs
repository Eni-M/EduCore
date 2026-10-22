using Microsoft.Extensions.Options;

namespace EduCore.Helpers;

public class GradingCalculator(IOptions<GradingOptions> options) : IGradingCalculator
{
    private readonly GradingOptions _options = options.Value;

    public double? WeightedValue(int? first, int? second) =>
        first.HasValue && second.HasValue
            ? first.Value * _options.FirstMarkWeight + second.Value * _options.SecondMarkWeight
            : null;

    public int? FinalMark(int? first, int? second) => FinalMark(WeightedValue(first, second));

    public int? FinalMark(double? weightedValue) =>
        weightedValue is null
            ? null
            : (int)Math.Round(weightedValue.Value, MidpointRounding.AwayFromZero);

    public int? AnnualMark(IEnumerable<int?> periodFinalMarks)
    {
        var graded = periodFinalMarks.Where(m => m.HasValue).Select(m => m!.Value).ToList();

        return graded.Count == 0
            ? null
            : (int)Math.Round(graded.Average(), MidpointRounding.AwayFromZero);
    }

    public double FirstMarkWeight => _options.FirstMarkWeight;
    public double SecondMarkWeight => _options.SecondMarkWeight;

    public double PassValue => _options.PassMark - 0.5;
    public double AtRiskLowerValue => _options.AtRiskMark - 0.5;
    public double AtRiskUpperValue => _options.AtRiskMark + 0.5;

    public bool IsPass(int finalMark) => finalMark >= _options.PassMark;
    public bool IsAtRisk(int finalMark) => finalMark == _options.AtRiskMark;

    public bool IsStudentAtRisk(double averageOfUnroundedMarks) =>
        averageOfUnroundedMarks < AtRiskUpperValue;
}
