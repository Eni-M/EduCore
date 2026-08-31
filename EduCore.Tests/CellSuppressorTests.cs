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
}
