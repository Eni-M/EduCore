namespace EduCore.Configuration;

// One token bucket. PermitsPerPeriod tokens are handed out every PeriodMinutes, and a caller
// with none left is rejected rather than queued -- a queued request just moves the delay.
public class RateLimitBucket
{
    public int PermitsPerPeriod { get; set; }
    public int PeriodMinutes { get; set; } = 1;
}

// Read at start-up rather than through IOptions: the limiter is configured while the container
// is still being built, before anything can be resolved from it.
public class RateLimitOptions
{
    public const string SectionName = "RateLimiting";

    // Every authenticated caller, or the address when there is no identity yet.
    public RateLimitBucket Global { get; set; } = new() { PermitsPerPeriod = 100, PeriodMinutes = 1 };

    // Stricter, because what is being stopped here is guessing a password.
    public RateLimitBucket Login { get; set; } = new() { PermitsPerPeriod = 5, PeriodMinutes = 1 };

    // Stricter still: this one sends mail to an address the caller names, so the abuse to stop
    // is flooding somebody else's inbox rather than guessing a secret.
    public RateLimitBucket PasswordReset { get; set; } = new() { PermitsPerPeriod = 3, PeriodMinutes = 15 };

    // What a rejected caller is told to wait.
    public int RetryAfterSeconds { get; set; } = 60;

    // Called by Program right after binding: IValidateOptions cannot reach a value read before the
    // container exists. A bucket of 0 permits would lock everybody out, silently.
    public IEnumerable<string> Validate()
    {
        foreach (var (name, bucket) in new[] { ("Global", Global), ("Login", Login), ("PasswordReset", PasswordReset) })
        {
            if (bucket.PermitsPerPeriod < 1)
                yield return $"{SectionName}:{name}:PermitsPerPeriod ({bucket.PermitsPerPeriod}) must be at least 1.";
            if (bucket.PeriodMinutes < 1)
                yield return $"{SectionName}:{name}:PeriodMinutes ({bucket.PeriodMinutes}) must be at least 1.";
        }

        if (RetryAfterSeconds < 1)
            yield return $"{SectionName}:RetryAfterSeconds ({RetryAfterSeconds}) must be at least 1.";
    }
}
