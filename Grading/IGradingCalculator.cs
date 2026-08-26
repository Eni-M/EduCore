namespace EduCore.Grading;

public interface IGradingCalculator
{
    double? WeightedValue(int? first, int? second);
    int? FinalMark(int? first, int? second);
    int? AnnualMark(IEnumerable<int?> periodFinalMarks);

    // Thresholds on the UNROUNDED weighted value. Queries compare against these instead of
    // rounding in SQL -- see GradingCalculator for why the two are equivalent.
    double PassValue { get; }
    double AtRiskLowerValue { get; }
    double AtRiskUpperValue { get; }

    bool IsPass(int finalMark);
    bool IsFail(int finalMark);
    bool IsAtRisk(int finalMark);

    // A student is at risk overall when the mean of their unrounded marks across every subject
    // has not cleared the at-risk band. Distinct from IsAtRisk, which judges one final mark.
    bool IsStudentAtRisk(double averageOfUnroundedMarks);
}
