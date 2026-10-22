using Microsoft.Extensions.Options;

namespace EduCore.Observability;

// Metrics ("is the system healthy") and traces ("why was this request slow"), switchable
// separately. Neither needs an external service: metrics are exposed locally and traces are
// collected in process unless an OTLP endpoint is configured.
public class ObservabilityOptions
{
    public const string SectionName = "Observability";

    // Names this service wherever more than one replica reports to the same place.
    public string ServiceName { get; set; } = "educore-api";

    public bool MetricsEnabled { get; set; } = true;

    // Prometheus text format; nothing needs installing to read it.
    public string MetricsPath { get; set; } = "/metrics";

    // Metrics name no individual but do reveal school size and usage, and the hosted API is on
    // the open internet. On by default, restricted to the national tier; a private monitoring
    // network that scrapes without a session (the compose stack) turns it off explicitly.
    public bool MetricsRequireAuthentication { get; set; } = true;

    public bool TracingEnabled { get; set; } = true;

    // Jaeger, Tempo, Grafana, Azure Monitor. Empty still collects in process, ships nothing.
    public string OtlpEndpoint { get; set; } = "";

    // 0.0 to 1.0. Everything is affordable at this volume; lower it if the collector is not.
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

        // Not redundant: TryCreate reads "localhost:4317" as scheme + path, so it passes and
        // then fails inside the exporter as traces silently going nowhere.
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
