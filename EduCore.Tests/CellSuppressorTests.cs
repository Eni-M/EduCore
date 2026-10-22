using EduCore.Helpers;
using EduCore.Dtos.Statistics;
using EduCore.Services.Statistics;
using Microsoft.Extensions.Options;
using Xunit;

namespace EduCore.Tests;

public class CellSuppressorTests
{
    private static CellSuppressor Suppressor(int minimum = 5) =>
        new(Options.Create(new PrivacyOptions { MinimumCellSize = minimum }));

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    public void BelowTheThreshold_IsSuppressed(int sampleSize)
    {
        var result = Suppressor().Apply(Measure.Of(3.5, sampleSize));

        Assert.True(result.Suppressed);
        Assert.Null(result.Value);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(50)]
    public void AtOrAboveTheThreshold_IsKept(int sampleSize)
    {
        var result = Suppressor().Apply(Measure.Of(3.5, sampleSize));

        Assert.False(result.Suppressed);
        Assert.Equal(3.5, result.Value);
    }

    [Fact]
    public void EmptySample_IsNotMarkedSuppressed()
    {
        var result = Suppressor().Apply(Measure.Of(null, 0));

        Assert.False(result.Suppressed);
        Assert.Null(result.Value);
    }

    [Fact]
    public void Suppression_AlsoClearsBaselineAndPrevious()
    {
        var measure = Measure.Of(3.5, 2).WithBaseline(3.8, "school average").WithPrevious(3.1);

        var result = Suppressor().Apply(measure);

        Assert.Null(result.Value);
        Assert.Null(result.Baseline);
        Assert.Null(result.Previous);

        Assert.Equal("school average", result.BaselineLabel);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(4, true)]
    [InlineData(5, false)]
    [InlineData(99, false)]
    public void ShouldSuppress_MatchesApply(int sampleSize, bool expected)
    {
        var suppressor = Suppressor();

        Assert.Equal(expected, suppressor.ShouldSuppress(sampleSize));
        Assert.Equal(expected, suppressor.Apply(Measure.Of(3.5, sampleSize)).Suppressed);
    }

    [Fact]
    public void ConfiguredMinimum_IsRespected()
    {
        Assert.False(Suppressor(minimum: 3).Apply(Measure.Of(3.5, 4)).Suppressed);
        Assert.True(Suppressor(minimum: 10).Apply(Measure.Of(3.5, 4)).Suppressed);
    }

    // What it was computed from is part of what is being withheld.
    [Fact]
    public void A_suppressed_measure_carries_no_sample_size() =>
        Assert.Equal(0, Suppressor().Apply(Measure.Of(80, 3)).SampleSize);

    private sealed record Row(string Name, Measure Rate);

    private static IReadOnlyList<Row> Complement(params Row[] rows) =>
        Suppressor().Complement(rows, r => r.Rate, (r, m) => r with { Rate = m });

    // One hidden class next to a published school total can be recovered by subtraction, so the
    // smallest published sibling is hidden with it.
    [Fact]
    public void A_lone_hidden_sibling_takes_the_smallest_published_one_with_it()
    {
        var s = Suppressor();
        var rows = Complement(
            new Row("A", s.Apply(Measure.Of(80, 3))),
            new Row("B", s.Apply(Measure.Of(70, 40))),
            new Row("C", s.Apply(Measure.Of(90, 12))));

        Assert.True(rows.Single(r => r.Name == "C").Rate.Suppressed);
        Assert.False(rows.Single(r => r.Name == "B").Rate.Suppressed);
    }

    [Fact]
    public void Two_hidden_siblings_need_no_partner()
    {
        var s = Suppressor();
        var rows = Complement(
            new Row("A", s.Apply(Measure.Of(80, 3))),
            new Row("B", s.Apply(Measure.Of(70, 2))),
            new Row("C", s.Apply(Measure.Of(90, 12))));

        Assert.False(rows.Single(r => r.Name == "C").Rate.Suppressed);
    }
}
