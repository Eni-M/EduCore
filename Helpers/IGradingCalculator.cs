namespace EduCore.Helpers;

public interface IGradingCalculator
{
    double? WeightedValue(int? first, int? second);
    int? FinalMark(int? first, int? second);

    // The final mark from a stored weighted value -- what every reader of recorded marks uses.
    int? FinalMark(double? weightedValue);
    int? AnnualMark(IEnumerable<int?> periodFinalMarks);

    double FirstMarkWeight { get; }
    double SecondMarkWeight { get; }

    double PassValue { get; }
    double AtRiskLowerValue { get; }
    double AtRiskUpperValue { get; }

    bool IsPass(int finalMark);
    bool IsAtRisk(int finalMark);

    bool IsStudentAtRisk(double averageOfUnroundedMarks);
}
