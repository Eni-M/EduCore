using EduCore.Helpers;
using EduCore.Models;
using EduCore.Repositories;
using Microsoft.Extensions.Options;
using Xunit;

namespace EduCore.Tests;

// The most valuable test in the suite.
//
// The grading rule exists in two forms: GradingCalculator in C#, and the query in
// MarkRepository.Value that Postgres runs, so aggregation does not pull every mark into
// memory. If those two drift, the database and the application disagree about a student's
// grade and NOTHING fails -- the numbers simply become wrong.
//
// This test runs the REAL query. MarkRepository.Value is executed here over an in-memory list,
// which evaluates the same expression tree EF would translate to SQL. Re-typing the formula
// into the test instead would compare one copy of the arithmetic against another and would
// keep passing after the real query changed.
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

    // Runs the production query rather than a copy of it.
    private static double QueryValue(int? first, int? second, GradingOptions? options = null)
    {
        var mark = new Mark { Id = Guid.NewGuid(), FirstMark = first, SecondMark = second };

        return MarkRepository
            .Value(new[] { mark }.AsQueryable(), options ?? Options)
            .Single()
            .Value;
    }

    [Theory]
    [MemberData(nameof(AllMarkPairs))]
    public void TheQuery_ComputesTheSameValueAsTheCalculator(int first, int second) =>
        Assert.Equal(Calculator().WeightedValue(first, second)!.Value, QueryValue(first, second), 10);

    // The query must drop half-graded marks, exactly as FinalMark returns null for them --
    // otherwise a missing second mark would silently count as a zero and drag averages down.
    [Theory]
    [InlineData(4, null)]
    [InlineData(null, 4)]
    [InlineData(null, null)]
    public void TheQuery_SkipsMarksTheCalculatorCannotGrade(int? first, int? second)
    {
        var mark = new Mark { Id = Guid.NewGuid(), FirstMark = first, SecondMark = second };

        Assert.Empty(MarkRepository.Value(new[] { mark }.AsQueryable(), Options));
        Assert.Null(Calculator().FinalMark(first, second));
    }

    // The identity the aggregation's pass/fail counting depends on: comparing the unrounded
    // value against PassValue must give the same answer as rounding and comparing to PassMark.
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

    // GetAtRiskAsync cannot call IsStudentAtRisk inside its query -- a method call does not
    // translate to SQL -- so it compares against AtRiskUpperValue directly. These must mean the
    // same thing, or the at-risk list and the rule disagree about who is at risk.
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

    // All of the above has to survive the weights being reconfigured, since that is the whole
    // reason they are configuration in the first place.
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
}
