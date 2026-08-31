using EduCore.Helpers;
using EduCore.Services.Statistics;
using Microsoft.Extensions.Options;
using Xunit;

namespace EduCore.Tests;

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

    [Fact]
    public void FourAndFive_RoundsUpToFive()
    {
        Assert.Equal(4.6, Calculator().WeightedValue(4, 5)!.Value, 10);
        Assert.Equal(5, Calculator().FinalMark(4, 5));
    }

    [Theory]
    [InlineData(1, 1, 1)]
    [InlineData(5, 5, 5)]
    [InlineData(5, 4, 4)]
    [InlineData(4, 5, 5)]
    [InlineData(1, 2, 2)]
    [InlineData(2, 1, 1)]
    public void FinalMark_UsesFortySixtyWeighting(int first, int second, int expected) =>
        Assert.Equal(expected, Calculator().FinalMark(first, second));

    [Fact]
    public void HalfValues_RoundAwayFromZero()
    {
        var calculator = Calculator(0.50, 0.50);

        Assert.Equal(2.5, calculator.WeightedValue(2, 3)!.Value, 10);
        Assert.Equal(3, calculator.FinalMark(2, 3));
    }

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
        Assert.Equal(4, Calculator().AnnualMark([3, 4]));

    [Fact]
    public void AnnualMark_SkipsPeriodsWithNoFinalMark() =>
        Assert.Equal(5, Calculator().AnnualMark([5, null]));

    [Fact]
    public void AnnualMark_IsNullWhenNoPeriodHasBeenGraded() =>
        Assert.Null(Calculator().AnnualMark([null, null]));

    [Fact]
    public void AnnualMark_IsNullForNoPeriodsAtAll() =>
        Assert.Null(Calculator().AnnualMark([]));

    [Theory]
    [InlineData(1, false, false)]
    [InlineData(2, true, true)]
    [InlineData(3, true, false)]
    [InlineData(5, true, false)]
    public void Classification_MatchesTheStateRules(int finalMark, bool expectPass, bool expectAtRisk)
    {
        var calculator = Calculator();

        Assert.Equal(expectPass, calculator.IsPass(finalMark));
        Assert.Equal(expectAtRisk, calculator.IsAtRisk(finalMark));
    }

    [Fact]
    public void ConfiguredWeights_AreActuallyUsed()
    {
        Assert.Equal(4, Calculator(0.60, 0.40).FinalMark(4, 5));
        Assert.Equal(5, Calculator(0.40, 0.60).FinalMark(4, 5));
    }
}
