using EduCore.Dtos.Statistics;
using EduCore.Services.Statistics;
using Microsoft.Extensions.Options;
using Xunit;

namespace EduCore.Tests;

// Suppression is the difference between publishing "grade 12 average: 2.1" and publishing the
// grades of the two girls in grade 12. Getting the boundary wrong is a privacy failure, not a
// rounding error.
public class CellSuppressorTests
{
    private static CellSuppressor Suppressor(int minimum = 5) =>
        new(Options.Create(new GradingOptions { MinimumCellSize = minimum }));

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

    // No data is not the same as data withheld, and callers may need to say so differently.
    [Fact]
    public void EmptySample_IsNotMarkedSuppressed()
    {
        var result = Suppressor().Apply(Measure.Of(null, 0));

        Assert.False(result.Suppressed);
        Assert.Null(result.Value);
    }

    // A baseline or a previous value would leak the same information the suppressed value was
    // hidden to protect.
    [Fact]
    public void Suppression_AlsoClearsBaselineAndPrevious()
    {
        var measure = Measure.Of(3.5, 2).WithBaseline(3.8, "school average").WithPrevious(3.1);

        var result = Suppressor().Apply(measure);

        Assert.Null(result.Value);
        Assert.Null(result.Baseline);
        Assert.Null(result.Previous);

        // The label survives, so the frontend can still explain what is missing and why.
        Assert.Equal("school average", result.BaselineLabel);
    }

    [Fact]
    public void ConfiguredMinimum_IsRespected()
    {
        Assert.False(Suppressor(minimum: 3).Apply(Measure.Of(3.5, 4)).Suppressed);
        Assert.True(Suppressor(minimum: 10).Apply(Measure.Of(3.5, 4)).Suppressed);
    }
}
