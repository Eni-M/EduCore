using EduCore.Dtos.Public;
using EduCore.Repositories.Interfaces;
using EduCore.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace EduCore.Services;

// The landing page is the one screen an anonymous visitor can reach, so its figures are cached
// rather than counted per request -- four aggregate scans is not something a public URL should
// be able to trigger at will. Ten minutes is far fresher than the figures actually change.
public class PlatformStatsService(IPlatformStatsRepository stats, IMemoryCache cache)
    : IPlatformStatsService
{
    private const string Key = "platform-stats";
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(10);

    public async Task<PlatformStatsDto> GetAsync(CancellationToken ct)
    {
        if (cache.TryGetValue(Key, out PlatformStatsDto? cached) && cached is not null)
            return cached;

        var fresh = await stats.GetAsync(ct);

        cache.Set(Key, fresh, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = Ttl });

        return fresh;
    }
}
