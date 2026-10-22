using EduCore.Security;
using Microsoft.Extensions.Options;
using Npgsql;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace EduCore.Observability;

public static class ObservabilityExtensions
{
    public static ObservabilityOptions AddEduCoreObservability(this WebApplicationBuilder builder)
    {
        builder.Services.AddOptions<ObservabilityOptions>()
            .Bind(builder.Configuration.GetSection(ObservabilityOptions.SectionName))
            .ValidateOnStart();
        builder.Services.AddSingleton<IValidateOptions<ObservabilityOptions>, ObservabilityOptionsValidator>();

        var options = builder.Configuration.GetSection(ObservabilityOptions.SectionName)
                          .Get<ObservabilityOptions>()
                      ?? new ObservabilityOptions();

        // Attached to every metric and every trace, so a figure can be traced back to the service
        // and version that produced it once more than one thing is reporting.
        var resource = ResourceBuilder.CreateDefault()
            .AddService(
                serviceName: options.ServiceName,
                serviceVersion: typeof(ObservabilityExtensions).Assembly.GetName().Version?.ToString(),
                serviceInstanceId: Environment.MachineName);

        var telemetry = builder.Services.AddOpenTelemetry().ConfigureResource(_ => { });

        if (options.MetricsEnabled)
        {
            telemetry.WithMetrics(metrics =>
            {
                metrics.SetResourceBuilder(resource)
                    // Request counts, durations and status codes per endpoint. This is what
                    // answers "how fast is the statistics page, really" with numbers instead of
                    // someone's impression of it.
                    .AddAspNetCoreInstrumentation()
                    // The .NET runtime itself: memory, garbage collection, thread pool. This is
                    // where a slow leak shows up days before it takes the process down.
                    .AddRuntimeInstrumentation()
                    // Npgsql publishes connection-pool and command counters. At this
                    // application's shape the database is the thing most likely to be the
                    // bottleneck, so these matter more than the rest.
                    .AddMeter("Npgsql")
                    // EF Core's own counters: queries, saves, compiled-query cache hits.
                    .AddMeter("Microsoft.EntityFrameworkCore")
                    .AddPrometheusExporter();
            });
        }

        if (options.TracingEnabled)
        {
            telemetry.WithTracing(tracing =>
            {
                tracing.SetResourceBuilder(resource)
                    .SetSampler(new TraceIdRatioBasedSampler(options.TraceSampleRatio))
                    .AddAspNetCoreInstrumentation(instrumentation =>
                    {
                        instrumentation.RecordException = true;

                        // The health check runs every 30 seconds forever and is never the thing
                        // being debugged; tracing it buries real requests in noise.
                        instrumentation.Filter = context =>
                            !context.Request.Path.StartsWithSegments("/healthz")
                            && !context.Request.Path.StartsWithSegments(options.MetricsPath);
                    })
                    // Every database statement a request ran, with its duration. This is the part
                    // that turns "the page is slow" into "it ran 2,500 statements".
                    .AddNpgsql();

                if (options.ShipsTraces)
                    tracing.AddOtlpExporter(exporter =>
                        exporter.Endpoint = new Uri(options.OtlpEndpoint));
            });
        }

        // Logs through the same pipeline as traces, so each record carries the trace and span id
        // of the request that wrote it and a collector can join the two. Console logging stays as
        // it is; this only adds the OTLP export when a collector is configured.
        if (options.ShipsTraces)
        {
            builder.Logging.AddOpenTelemetry(logging =>
            {
                logging.SetResourceBuilder(resource);
                logging.IncludeFormattedMessage = true;
                logging.IncludeScopes = true;
                logging.AddOtlpExporter(exporter => exporter.Endpoint = new Uri(options.OtlpEndpoint));
            });
        }

        return options;
    }

    public static void MapEduCoreMetrics(this WebApplication app, ObservabilityOptions options)
    {
        if (!options.MetricsEnabled)
            return;

        var metrics = app.MapPrometheusScrapingEndpoint(options.MetricsPath);

        // Never rate-limited: a scraper polls on a fixed interval, and throttling it produces
        // gaps in the graphs at exactly the busy moments the graphs exist for.
        metrics.DisableRateLimiting();

        if (options.MetricsRequireAuthentication)
            metrics.RequireAuthorization(Policies.StateAdmin);
    }
}
