using Microsoft.Extensions.Options;

namespace EduCore.Observability;

/// <summary>
/// What the application reports about itself while it runs, and where that goes.
///
/// Two separate things, deliberately separately switchable:
///
/// Metrics are counters and timings sampled continuously -- how many requests, how long they
/// took, how many failed, how much memory the process is using. They answer "is the system
/// healthy right now, and is it getting worse", and they are cheap enough to always leave on.
///
/// Traces follow one single request through the application -- which endpoint, which database
/// statements it ran, how long each took, and the exception if it failed. They answer "why was
/// *this* request slow, or why did it break", which no amount of aggregate metrics can tell you.
///
/// Neither needs an external service to be useful. Metrics are exposed on a local endpoint that
/// any dashboard can read, and traces are only shipped anywhere if an OTLP endpoint is
/// configured -- so this defaults to something that runs anywhere, including a machine with no
/// monitoring stack in front of it yet.
/// </summary>
public class ObservabilityOptions
{
    public const string SectionName = "Observability";

    // Names this service in whatever collects from it. Matters as soon as more than one service,
    // or more than one replica, reports to the same place.
    public string ServiceName { get; set; } = "educore-api";

    public bool MetricsEnabled { get; set; } = true;

    // Where the numbers are exposed, in the text format Prometheus and Grafana Alloy scrape.
    // Nothing needs to be installed for this to work; it is a page of counters over HTTP.
    public string MetricsPath { get; set; } = "/metrics";

    // Metrics describe the whole system and name no individual, but they still say how big each
    // school is and how heavily it is used. Off by default; the endpoint is meant to be reachable
    // from the monitoring network, not the internet.
    public bool MetricsRequireAuthentication { get; set; } = false;

    public bool TracingEnabled { get; set; } = true;

    // An OTLP collector -- Jaeger, Tempo, Grafana, Azure Monitor, most vendors. Empty means
    // traces are still collected in-process (so a trace id in a log line is still a real trace)
    // but nothing is shipped anywhere. Set this when there is somewhere to send it.
    public string OtlpEndpoint { get; set; } = "";

    // Fraction of requests traced, 0.0 to 1.0. Tracing every request is affordable at this
    // application's volume and makes debugging a specific complaint possible; lower it if the
    // collector becomes the expensive part.
    public double TraceSampleRatio { get; set; } = 1.0;

    public bool ShipsTraces => !string.IsNullOrWhiteSpace(OtlpEndpoint);
}

public class ObservabilityOptionsValidator : IValidateOptions<ObservabilityOptions>
{
    public ValidateOptionsResult Validate(string? name, ObservabilityOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.ServiceName))
            failures.Add("Observability:ServiceName must not be empty -- it is how this service is "
                         + "identified in whatever collects from it.");

        if (options.MetricsEnabled && !options.MetricsPath.StartsWith('/'))
            failures.Add($"Observability:MetricsPath ('{options.MetricsPath}') must start with '/'.");

        if (options.TraceSampleRatio is < 0 or > 1)
            failures.Add($"Observability:TraceSampleRatio ({options.TraceSampleRatio}) must be "
                         + "between 0.0 (trace nothing) and 1.0 (trace every request).");

        // The scheme check is not redundant. Uri.TryCreate accepts "localhost:4317" as absolute,
        // parsing "localhost" as the scheme and "4317" as the path -- so the obvious way to write
        // this address passes a bare TryCreate and then fails inside the exporter, where it
        // surfaces as traces silently going nowhere.
        if (options.ShipsTraces
            && (!Uri.TryCreate(options.OtlpEndpoint, UriKind.Absolute, out var otlp)
                || (otlp.Scheme != Uri.UriSchemeHttp && otlp.Scheme != Uri.UriSchemeHttps)))
            failures.Add($"Observability:OtlpEndpoint ('{options.OtlpEndpoint}') must be an "
                         + "absolute http or https URL, for example http://localhost:4317.");

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
