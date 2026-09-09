using EduCore.Observability;
using Xunit;

namespace EduCore.Tests;

// Observability is the thing nobody notices is misconfigured until the moment they need it --
// during an incident, when the graphs are empty and the traces went nowhere. The validator turns
// each of those into a start-up failure instead.
public class ObservabilityOptionsTests
{
    private static (bool Succeeded, string Failures) Validate(ObservabilityOptions options)
    {
        var result = new ObservabilityOptionsValidator().Validate(null, options);

        return (result.Succeeded, string.Join(" | ", result.Failures ?? []));
    }

    [Fact]
    public void Defaults_are_valid_and_need_no_external_service()
    {
        // The point of the defaults: metrics and traces work on a machine with no monitoring
        // stack in front of it, so a trace id in an error response is always a real trace.
        var options = new ObservabilityOptions();

        Assert.True(Validate(options).Succeeded);
        Assert.True(options.MetricsEnabled);
        Assert.True(options.TracingEnabled);
        Assert.False(options.ShipsTraces);
    }

    [Fact]
    public void Traces_are_shipped_only_once_a_collector_is_configured()
    {
        Assert.False(new ObservabilityOptions { OtlpEndpoint = "" }.ShipsTraces);
        Assert.False(new ObservabilityOptions { OtlpEndpoint = "   " }.ShipsTraces);
        Assert.True(new ObservabilityOptions { OtlpEndpoint = "http://localhost:4317" }.ShipsTraces);
    }

    [Fact]
    public void An_empty_service_name_is_rejected()
    {
        var (succeeded, failures) = Validate(new ObservabilityOptions { ServiceName = "" });

        Assert.False(succeeded);
        Assert.Contains("ServiceName", failures);
    }

    [Fact]
    public void A_metrics_path_that_is_not_a_path_is_rejected()
    {
        var (succeeded, failures) = Validate(new ObservabilityOptions { MetricsPath = "metrics" });

        Assert.False(succeeded);
        Assert.Contains("MetricsPath", failures);
    }

    [Fact]
    public void The_metrics_path_is_not_checked_when_metrics_are_off()
    {
        var options = new ObservabilityOptions { MetricsEnabled = false, MetricsPath = "nonsense" };

        Assert.True(Validate(options).Succeeded);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void A_sample_ratio_outside_zero_to_one_is_rejected(double ratio)
    {
        var (succeeded, failures) = Validate(new ObservabilityOptions { TraceSampleRatio = ratio });

        Assert.False(succeeded);
        Assert.Contains("TraceSampleRatio", failures);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.05)]
    [InlineData(1.0)]
    public void The_ends_of_the_sampling_range_are_allowed(double ratio)
    {
        Assert.True(Validate(new ObservabilityOptions { TraceSampleRatio = ratio }).Succeeded);
    }

    [Fact]
    public void A_collector_address_that_is_not_a_url_is_rejected()
    {
        // Caught at start-up rather than discovered as silently missing traces.
        var (succeeded, failures) = Validate(new ObservabilityOptions { OtlpEndpoint = "localhost:4317" });

        Assert.False(succeeded);
        Assert.Contains("OtlpEndpoint", failures);
    }
}
