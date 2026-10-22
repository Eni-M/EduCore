using EduCore.Helpers;
using EduCore.Models;
using EduCore.Repositories;
using Microsoft.Extensions.Options;
using Xunit;

namespace EduCore.Tests;

public class GradingRuleAgreementTests
{
    private static readonly GradingOptions Options = new();

    private static GradingCalculator Calculator(GradingOptions? options = null) =>
        new(Microsoft.Extensions.Options.Options.Create(options ?? Options));

    public static TheoryData<int, int> AllMarkPairs()
    {
        var data = new TheoryData<int, int>();

        foreach (var first in Enumerable.Range(1, 5))
        foreach (var second in Enumerable.Range(1, 5))
            data.Add(first, second);

        return data;
    }

    // A mark as MarkService writes it: the weighted value computed once, under the weights in
    // force at the time, and stored. The queries read it back rather than recomputing.
    private static Mark Written(int? first, int? second, GradingOptions? options = null) => new()
    {
        Id = Guid.NewGuid(),
        FirstMark = first,
        SecondMark = second,
        WeightedValue = Calculator(options).WeightedValue(first, second)
    };

    private static double QueryValue(int? first, int? second, GradingOptions? options = null) =>
        MarkRepository
            .ValueOf(new[] { Written(first, second, options) }.AsQueryable())
            .Single()
            .Value;

    [Theory]
    [MemberData(nameof(AllMarkPairs))]
    public void TheQuery_ComputesTheSameValueAsTheCalculator(int first, int second) =>
        Assert.Equal(Calculator().WeightedValue(first, second)!.Value, QueryValue(first, second), 10);

    [Theory]
    [InlineData(4, null)]
    [InlineData(null, 4)]
    [InlineData(null, null)]
    public void TheQuery_SkipsMarksTheCalculatorCannotGrade(int? first, int? second)
    {
        Assert.Empty(MarkRepository.ValueOf(new[] { Written(first, second) }.AsQueryable()));
        Assert.Null(Calculator().FinalMark(first, second));
    }

    [Theory]
    [MemberData(nameof(AllMarkPairs))]
    public void PassValueThreshold_AgreesWithRoundedClassification(int first, int second)
    {
        var calculator = Calculator();

        var byRoundedMark = calculator.IsPass(calculator.FinalMark(first, second)!.Value);
        var byThreshold = QueryValue(first, second) >= calculator.PassValue;

        Assert.Equal(byRoundedMark, byThreshold);
    }

    [Theory]
    [MemberData(nameof(AllMarkPairs))]
    public void AtRiskBand_AgreesWithRoundedClassification(int first, int second)
    {
        var calculator = Calculator();
        var value = QueryValue(first, second);

        var byRoundedMark = calculator.IsAtRisk(calculator.FinalMark(first, second)!.Value);
        var byBand = value >= calculator.AtRiskLowerValue && value < calculator.AtRiskUpperValue;

        Assert.Equal(byRoundedMark, byBand);
    }

    [Theory]
    [InlineData(1.0, true)]
    [InlineData(2.49, true)]
    [InlineData(2.5, false)]
    [InlineData(4.0, false)]
    public void StudentAtRiskRule_MatchesTheThresholdTheQueryUses(double average, bool expected)
    {
        var calculator = Calculator();

        Assert.Equal(expected, calculator.IsStudentAtRisk(average));
        Assert.Equal(expected, average < calculator.AtRiskUpperValue);
    }

    [Theory]
    [InlineData(0.50, 0.50, 3)]
    [InlineData(0.30, 0.70, 2)]
    [InlineData(0.40, 0.60, 4)]
    public void EverythingHolds_ForOtherConfigurations(
        double firstWeight, double secondWeight, int passMark)
    {
        var options = new GradingOptions
        {
            FirstMarkWeight = firstWeight,
            SecondMarkWeight = secondWeight,
            PassMark = passMark,
            AtRiskMark = passMark
        };

        var calculator = Calculator(options);

        foreach (var first in Enumerable.Range(1, 5))
        foreach (var second in Enumerable.Range(1, 5))
        {
            var value = QueryValue(first, second, options);

            Assert.Equal(calculator.WeightedValue(first, second)!.Value, value, 10);
            Assert.Equal(
                calculator.IsPass(calculator.FinalMark(first, second)!.Value),
                value >= calculator.PassValue);
        }
    }

    // The point of storing it: a later change to the Grading weights must not rewrite a mark
    // that was recorded under the old ones.
    [Fact]
    public void Changing_the_weights_does_not_rewrite_a_mark_already_recorded()
    {
        var recorded = Written(1, 5);
        var reweighted = Calculator(new GradingOptions { FirstMarkWeight = 0.7, SecondMarkWeight = 0.3 });

        Assert.Equal(3, Calculator().FinalMark(recorded.WeightedValue));
        Assert.Equal(3, reweighted.FinalMark(recorded.WeightedValue));
        Assert.Equal(2, reweighted.FinalMark(1, 5));
    }
}
