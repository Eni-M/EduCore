using EduCore.Services.Statistics;
using Microsoft.Extensions.Options;
using Xunit;

namespace EduCore.Tests;

// The grading rule is the one piece of this system where a defect produces a plausible wrong
// number instead of a visible failure -- a wrong pass rate looks exactly like a right one.
public class GradingCalculatorTests
{
    private static GradingCalculator Calculator(
        double firstWeight = 0.40, double secondWeight = 0.60, int passMark = 2, int atRiskMark = 2) =>
        new(Options.Create(new GradingOptions
        {
            FirstMarkWeight = firstWeight,
            SecondMarkWeight = secondWeight,
            PassMark = passMark,
            AtRiskMark = atRiskMark
        }));

    // The worked example the rule was specified with: an exam of 4 and coursework of 5 gives
    // 4.6, which is a 5.
    [Fact]
    public void FourAndFive_RoundsUpToFive()
    {
        Assert.Equal(4.6, Calculator().WeightedValue(4, 5)!.Value, 10);
        Assert.Equal(5, Calculator().FinalMark(4, 5));
    }

    [Theory]
    [InlineData(1, 1, 1)]
    [InlineData(5, 5, 5)]
    [InlineData(5, 4, 4)]  // 4.4 rounds down
    [InlineData(4, 5, 5)]  // 4.6 rounds up
    [InlineData(1, 2, 2)]  // 1.6 rounds up
    [InlineData(2, 1, 1)]  // 1.4 rounds down
    public void FinalMark_UsesFortySixtyWeighting(int first, int second, int expected) =>
        Assert.Equal(expected, Calculator().FinalMark(first, second));

    // Exactly .5 must round away from zero, not to even. Banker's rounding would turn a 2.5
    // into a 2 and quietly fail a student who should have passed with a 3.
    [Fact]
    public void HalfValues_RoundAwayFromZero()
    {
        var calculator = Calculator(0.50, 0.50);

        Assert.Equal(2.5, calculator.WeightedValue(2, 3)!.Value, 10);
        Assert.Equal(3, calculator.FinalMark(2, 3));
    }

    // A period still in progress must read as incomplete, not as a low grade -- otherwise a
    // mid-term dashboard shows every class failing.
    [Theory]
    [InlineData(4, null)]
    [InlineData(null, 4)]
    [InlineData(null, null)]
    public void FinalMark_IsNullUntilBothMarksExist(int? first, int? second)
    {
        Assert.Null(Calculator().FinalMark(first, second));
        Assert.Null(Calculator().WeightedValue(first, second));
    }

    [Fact]
    public void AnnualMark_IsTheMeanOfPeriodFinals() =>
        Assert.Equal(4, Calculator().AnnualMark([3, 4]));  // 3.5 rounds away from zero

    [Fact]
    public void AnnualMark_SkipsPeriodsWithNoFinalMark() =>
        Assert.Equal(5, Calculator().AnnualMark([5, null]));

    [Fact]
    public void AnnualMark_IsNullWhenNoPeriodHasBeenGraded() =>
        Assert.Null(Calculator().AnnualMark([null, null]));

    [Fact]
    public void AnnualMark_IsNullForNoPeriodsAtAll() =>
        Assert.Null(Calculator().AnnualMark([]));

    // 1 fails, 2 and above pass, exactly 2 is at risk.
    [Theory]
    [InlineData(1, false, false)]
    [InlineData(2, true, true)]
    [InlineData(3, true, false)]
    [InlineData(5, true, false)]
    public void Classification_MatchesTheStateRules(int finalMark, bool expectPass, bool expectAtRisk)
    {
        var calculator = Calculator();

        Assert.Equal(expectPass, calculator.IsPass(finalMark));
        Assert.Equal(!expectPass, calculator.IsFail(finalMark));
        Assert.Equal(expectAtRisk, calculator.IsAtRisk(finalMark));
    }

    // The weights are configuration precisely because the 40/60 split is believed correct
    // rather than known to be. Changing them must change the result and nothing else.
    [Fact]
    public void ConfiguredWeights_AreActuallyUsed()
    {
        Assert.Equal(4, Calculator(0.60, 0.40).FinalMark(4, 5));  // 4.4 rounds down
        Assert.Equal(5, Calculator(0.40, 0.60).FinalMark(4, 5));  // 4.6 rounds up
    }
}
