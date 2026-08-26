using EduCore.Services.Statistics;
using Microsoft.Extensions.Options;
using Xunit;

namespace EduCore.Tests;

// The most valuable test in the suite.
//
// The grading rule exists in two forms: GradingCalculator in C#, and a LINQ query in
// MarkRepository.Value that Postgres runs, so aggregation does not pull every mark into
// memory. If those two drift, the database and the application disagree about a student's
// grade and NOTHING fails -- the numbers simply become wrong.
//
// The query cannot call GradingCalculator (a method call does not translate to SQL), so it
// relies on one identity instead: rounding half-away-from-zero means a rounded mark reaches
// PassMark exactly when the unrounded value reaches GradingOptions.PassValue. These tests pin
// that identity across every possible pair of marks.
public class GradingRuleAgreementTests
{
    private static readonly GradingOptions Options = new();

    private static GradingCalculator Calculator() =>
        new(Microsoft.Extensions.Options.Options.Create(Options));

    public static TheoryData<int, int> AllMarkPairs()
    {
        var data = new TheoryData<int, int>();

        foreach (var first in Enumerable.Range(1, 5))
        foreach (var second in Enumerable.Range(1, 5))
            data.Add(first, second);

        return data;
    }

    // What the repository computes in SQL.
    private static double RepositoryWeightedValue(int first, int second) =>
        first * Options.FirstMarkWeight + second * Options.SecondMarkWeight;

    [Theory]
    [MemberData(nameof(AllMarkPairs))]
    public void RepositoryWeighting_MatchesCalculatorWeighting(int first, int second) =>
        Assert.Equal(
            Calculator().WeightedValue(first, second)!.Value,
            RepositoryWeightedValue(first, second),
            10);

    // The identity the repository's pass/fail counting depends on.
    [Theory]
    [MemberData(nameof(AllMarkPairs))]
    public void RepositoryPassThreshold_AgreesWithRoundedClassification(int first, int second)
    {
        var calculator = Calculator();

        var passesByRoundedMark = calculator.IsPass(calculator.FinalMark(first, second)!.Value);
        var passesByThreshold = RepositoryWeightedValue(first, second) >= Options.PassValue;

        Assert.Equal(passesByRoundedMark, passesByThreshold);
    }

    [Theory]
    [MemberData(nameof(AllMarkPairs))]
    public void RepositoryAtRiskBand_AgreesWithRoundedClassification(int first, int second)
    {
        var calculator = Calculator();
        var value = RepositoryWeightedValue(first, second);

        var atRiskByRoundedMark = calculator.IsAtRisk(calculator.FinalMark(first, second)!.Value);
        var atRiskByBand = value >= Options.AtRiskLowerValue && value < Options.AtRiskUpperValue;

        Assert.Equal(atRiskByRoundedMark, atRiskByBand);
    }

    // The identity has to survive the weights being reconfigured, since that is the whole
    // reason they live in configuration.
    [Theory]
    [InlineData(0.50, 0.50, 3)]
    [InlineData(0.30, 0.70, 2)]
    [InlineData(0.40, 0.60, 4)]
    public void ThresholdIdentity_HoldsForOtherConfigurations(
        double firstWeight, double secondWeight, int passMark)
    {
        var options = new GradingOptions
        {
            FirstMarkWeight = firstWeight,
            SecondMarkWeight = secondWeight,
            PassMark = passMark,
            AtRiskMark = passMark
        };

        var calculator = new GradingCalculator(Microsoft.Extensions.Options.Options.Create(options));

        foreach (var first in Enumerable.Range(1, 5))
        foreach (var second in Enumerable.Range(1, 5))
        {
            var value = first * firstWeight + second * secondWeight;

            Assert.Equal(
                calculator.IsPass(calculator.FinalMark(first, second)!.Value),
                value >= options.PassValue);
        }
    }
}
