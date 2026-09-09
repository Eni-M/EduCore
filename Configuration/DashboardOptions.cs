using Microsoft.Extensions.Options;

namespace EduCore.Configuration;

// How much the one dashboard endpoint is willing to assemble. Both numbers bound work a single
// request can cause, not just how much it displays.
public class DashboardOptions
{
    public const string SectionName = "Dashboard";

    // Enough to see the shape of recent activity without turning the dashboard into a list page.
    public int RecentActivityLimit { get; set; } = 5;

    // A guardian with more children than this gets the first few; the rest are one click away in
    // the portal. The per-child lookups are sequential, so this bounds the work per request.
    public int MaxChildrenDetailed { get; set; } = 4;
}

public class DashboardOptionsValidator : IValidateOptions<DashboardOptions>
{
    public ValidateOptionsResult Validate(string? name, DashboardOptions options) =>
        options.RecentActivityLimit is < 1 or > 50
            ? ValidateOptionsResult.Fail(
                $"Dashboard:RecentActivityLimit ({options.RecentActivityLimit}) must be between 1 and 50.")
            : options.MaxChildrenDetailed is < 1 or > 20
                ? ValidateOptionsResult.Fail(
                    $"Dashboard:MaxChildrenDetailed ({options.MaxChildrenDetailed}) must be between 1 and 20. "
                    + "Each one costs its own round of queries on a single request.")
                : ValidateOptionsResult.Success;
}
