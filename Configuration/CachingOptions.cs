using Microsoft.Extensions.Options;

namespace EduCore.Configuration;

// How long the two in-memory caches hold a value. Both trade freshness for load, and both are
// safe to shorten and dangerous to lengthen without thinking, so neither belongs buried in a
// static readonly field.
public class CachingOptions
{
    public const string SectionName = "Caching";

    // Whole-platform figures on the public landing page. Six aggregate scans, reachable by
    // anyone with the URL, so this is as much a rate limit as a cache.
    public int PlatformStatsMinutes { get; set; } = 10;

    // The per-request "is this account still allowed in" lookup. Revocation does not wait for
    // this to expire -- the write that deactivates an account evicts the entry -- so the window
    // only matters when a second instance writes the same database.
    public int AccountStatusSeconds { get; set; } = 60;
}

public class CachingOptionsValidator : IValidateOptions<CachingOptions>
{
    public ValidateOptionsResult Validate(string? name, CachingOptions options)
    {
        if (options.PlatformStatsMinutes is < 1 or > 1440)
            return ValidateOptionsResult.Fail(
                $"Caching:PlatformStatsMinutes ({options.PlatformStatsMinutes}) must be between 1 and 1440. "
                + "Below a minute the cache stops being one; above a day the landing page is quoting "
                + "figures from yesterday as live.");

        return options.AccountStatusSeconds is < 5 or > 600
            ? ValidateOptionsResult.Fail(
                $"Caching:AccountStatusSeconds ({options.AccountStatusSeconds}) must be between 5 and 600. "
                + "This is how long a deactivated account can still be let in by another instance "
                + "that did not see the eviction, so it is not free to raise.")
            : ValidateOptionsResult.Success;
    }
}
