using EduCore.Dtos.Public;
using EduCore.Repositories.Interfaces;
using EduCore.Services.Interfaces;
using EduCore.Services.Statistics.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace EduCore.Services;

// The landing page is the one screen an anonymous visitor can reach, so its figures are cached
// rather than counted per request -- six aggregate scans is not something a public URL should be
// able to trigger at will. Ten minutes is far fresher than these figures actually change.
//
// Counting lives in the repository; deciding what may be published lives here, which is where
// the statistics services draw the same line.
public class PlatformStatsService(
    IPlatformStatsRepository stats,
    ICellSuppressor suppressor,
    IMemoryCache cache) : IPlatformStatsService
{
    private const string Key = "platform-stats";
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(10);

    public async Task<PlatformStatsDto> GetAsync(CancellationToken ct)
    {
        if (cache.TryGetValue(Key, out PlatformStatsDto? cached) && cached is not null)
            return cached;

        var counted = await stats.GetAsync(ct);

        // The same threshold and the same rule the statistics screens use: a figure below
        // Privacy:MinimumCellSize is withheld, and zero is left alone because "none recorded
        // yet" is a fact rather than a small sample.
        var fresh = new PlatformStatsDto(
            Publishable(counted.Schools),
            Publishable(counted.Cities),
            Publishable(counted.Students),
            Publishable(counted.Teachers),
            Publishable(counted.Lessons),
            Publishable(counted.Attendances));

        cache.Set(Key, fresh, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = Ttl });

        return fresh;
    }

    private int? Publishable(int? count) =>
        count is { } n && suppressor.ShouldSuppress(n) ? null : count;
}
