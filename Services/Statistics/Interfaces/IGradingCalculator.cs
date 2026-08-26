namespace EduCore.Services.Statistics.Interfaces;

public interface IGradingCalculator
{
    double? WeightedValue(int? first, int? second);
    int? FinalMark(int? first, int? second);
    int? AnnualMark(IEnumerable<int?> periodFinalMarks);
    bool IsPass(int finalMark);
    bool IsFail(int finalMark);
    bool IsAtRisk(int finalMark);
}
