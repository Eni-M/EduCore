using EduCore.Configuration;
using EduCore.Dtos.Public;
using EduCore.Repositories.Interfaces;
using EduCore.Services;
using EduCore.Services.Statistics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Xunit;

namespace EduCore.Tests;

// The public stats endpoint is the only thing an anonymous visitor can reach, so what it is
// willing to publish is worth pinning down. It reuses the statistics screens' own suppressor
// rather than carrying a second threshold that could drift away from Privacy:MinimumCellSize.
public class PlatformStatsSuppressionTests
{
    private sealed class FixedStatsRepository(PlatformStatsDto counts) : IPlatformStatsRepository
    {
        public Task<PlatformStatsDto> GetAsync(CancellationToken ct) => Task.FromResult(counts);
    }

    private static PlatformStatsService Service(PlatformStatsDto counts, int minimumCellSize = 5) =>
        new(new FixedStatsRepository(counts),
            new CellSuppressor(Options.Create(new PrivacyOptions { MinimumCellSize = minimumCellSize })),
            Options.Create(new CachingOptions()),
            new MemoryCache(new MemoryCacheOptions()));

    [Fact]
    public async Task Counts_below_the_minimum_cell_size_are_withheld()
    {
        var service = Service(new PlatformStatsDto(80, 20, 5008, 701, Lessons: 2, Attendances: 1));

        var result = await service.GetAsync(TestContext.Current.CancellationToken);

        Assert.Null(result.Lessons);
        Assert.Null(result.Attendances);

        // Everything at or above the threshold is published unchanged.
        Assert.Equal(80, result.Schools);
        Assert.Equal(20, result.Cities);
        Assert.Equal(5008, result.Students);
        Assert.Equal(701, result.Teachers);
    }

    [Fact]
    public async Task A_count_of_zero_is_published_rather_than_suppressed()
    {
        // "Nothing recorded yet" is a fact about the platform, not a small sample that could
        // identify somebody. CellSuppressor draws the same distinction.
        var service = Service(new PlatformStatsDto(0, 0, 0, 0, 0, 0));

        var result = await service.GetAsync(TestContext.Current.CancellationToken);

        Assert.Equal(0, result.Lessons);
        Assert.Equal(0, result.Attendances);
        Assert.Equal(0, result.Schools);
    }

    [Fact]
    public async Task The_count_exactly_on_the_threshold_is_published()
    {
        var service = Service(new PlatformStatsDto(5, 5, 5, 5, 5, 5), minimumCellSize: 5);

        var result = await service.GetAsync(TestContext.Current.CancellationToken);

        Assert.Equal(5, result.Lessons);
        Assert.Equal(5, result.Attendances);
    }

    [Fact]
    public async Task The_threshold_comes_from_configuration_not_from_a_constant_here()
    {
        // Raising Privacy:MinimumCellSize has to tighten the public endpoint too, otherwise the
        // landing page becomes the one place that ignores the platform's privacy setting.
        var counts = new PlatformStatsDto(80, 20, 5008, 701, 40, 40);

        var lenient = await Service(counts, minimumCellSize: 5).GetAsync(TestContext.Current.CancellationToken);
        var strict = await Service(counts, minimumCellSize: 100).GetAsync(TestContext.Current.CancellationToken);

        Assert.Equal(40, lenient.Lessons);
        Assert.Null(strict.Lessons);
        Assert.Null(strict.Schools);
    }
}
