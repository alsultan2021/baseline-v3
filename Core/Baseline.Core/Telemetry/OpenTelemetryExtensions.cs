using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Baseline.Core.Telemetry;

/// <summary>
/// OpenTelemetry configuration for Baseline v3.
/// Provides automatic instrumentation for content queries, caching, and Page Builder operations.
/// </summary>
public static class OpenTelemetryExtensions
{
    /// <summary>
    /// Activity source for Baseline operations.
    /// </summary>
    public static readonly ActivitySource BaselineActivitySource = new("Baseline.Core", "3.0.0");

    /// <summary>
    /// Adds OpenTelemetry tracing and metrics for Baseline operations.
    /// </summary>
    public static IServiceCollection AddBaselineOpenTelemetry(
        this IServiceCollection services,
        Action<BaselineOpenTelemetryOptions>? configure = null)
    {
        var options = new BaselineOpenTelemetryOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddSingleton<IBaselineTelemetry, BaselineTelemetry>();

        // Configure OpenTelemetry
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(
                    serviceName: options.ServiceName,
                    serviceVersion: options.ServiceVersion))
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(BaselineActivitySource.Name)
                    .AddSource("CMS.ContentEngine")
                    .AddAspNetCoreInstrumentation(o =>
                    {
                        o.RecordException = true;
                        o.Filter = httpContext =>
                        {
                            // Skip health checks and static files
                            var path = httpContext.Request.Path.Value ?? "";
                            return !path.StartsWith("/health") &&
                                   !path.StartsWith("/status") &&
                                   !path.StartsWith("/_") &&
                                   !path.Contains("/static/");
                        };
                    })
                    .AddHttpClientInstrumentation();

                if (options.EnableConsoleExporter)
                {
                    tracing.AddConsoleExporter();
                }

                if (!string.IsNullOrEmpty(options.OtlpEndpoint))
                {
                    tracing.AddOtlpExporter(otlp =>
                    {
                        otlp.Endpoint = new Uri(options.OtlpEndpoint);
                    });
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddMeter(BaselineMetrics.MeterName)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();

                if (options.EnableConsoleExporter)
                {
                    metrics.AddConsoleExporter();
                }

                if (!string.IsNullOrEmpty(options.OtlpEndpoint))
                {
                    metrics.AddOtlpExporter(otlp =>
                    {
                        otlp.Endpoint = new Uri(options.OtlpEndpoint);
                    });
                }
            });

        return services;
    }
}

/// <summary>
/// Configuration options for Baseline OpenTelemetry.
/// </summary>
public class BaselineOpenTelemetryOptions
{
    /// <summary>
    /// Service name for telemetry. Default: "Baseline.Web"
    /// </summary>
    public string ServiceName { get; set; } = "Baseline.Web";

    /// <summary>
    /// Service version. Default: "3.0.0"
    /// </summary>
    public string ServiceVersion { get; set; } = "3.0.0";

    /// <summary>
    /// OTLP endpoint for exporting telemetry (e.g., "http://localhost:4317").
    /// </summary>
    public string? OtlpEndpoint { get; set; }

    /// <summary>
    /// Enable console exporter for debugging. Default: false
    /// </summary>
    public bool EnableConsoleExporter { get; set; }

    /// <summary>
    /// Enable content query tracing. Default: true
    /// </summary>
    public bool TraceContentQueries { get; set; } = true;

    /// <summary>
    /// Enable cache operation tracing. Default: true
    /// </summary>
    public bool TraceCacheOperations { get; set; } = true;

    /// <summary>
    /// Enable Page Builder operation tracing. Default: true
    /// </summary>
    public bool TracePageBuilder { get; set; } = true;
}

/// <summary>
/// Interface for Baseline telemetry operations.
/// </summary>
public interface IBaselineTelemetry
{
    /// <summary>
    /// Start a content query activity.
    /// </summary>
    Activity? StartContentQuery(string contentType, string? cacheKey = null);

    /// <summary>
    /// Start a cache operation activity.
    /// </summary>
    Activity? StartCacheOperation(string operation, string cacheKey);

    /// <summary>
    /// Start a Page Builder render activity.
    /// </summary>
    Activity? StartPageBuilderRender(string componentType, string componentId);

    /// <summary>
    /// Record a content query metric.
    /// </summary>
    void RecordContentQuery(string contentType, int resultCount, TimeSpan duration, bool fromCache);

    /// <summary>
    /// Record a cache hit/miss.
    /// </summary>
    void RecordCacheAccess(string cacheKey, bool isHit);
}

/// <summary>
/// Default implementation of Baseline telemetry.
/// </summary>
public class BaselineTelemetry : IBaselineTelemetry
{
    private readonly BaselineOpenTelemetryOptions _options;

    public BaselineTelemetry(BaselineOpenTelemetryOptions options)
    {
        _options = options;
    }

    public Activity? StartContentQuery(string contentType, string? cacheKey = null)
    {
        if (!_options.TraceContentQueries)
            return null;

        var activity = OpenTelemetryExtensions.BaselineActivitySource.StartActivity(
            "ContentQuery",
            ActivityKind.Internal);

        activity?.SetTag("baseline.content_type", contentType);
        activity?.SetTag("baseline.cache_key", cacheKey);

        return activity;
    }

    public Activity? StartCacheOperation(string operation, string cacheKey)
    {
        if (!_options.TraceCacheOperations)
            return null;

        var activity = OpenTelemetryExtensions.BaselineActivitySource.StartActivity(
            $"Cache.{operation}",
            ActivityKind.Internal);

        activity?.SetTag("baseline.cache_operation", operation);
        activity?.SetTag("baseline.cache_key", cacheKey);

        return activity;
    }

    public Activity? StartPageBuilderRender(string componentType, string componentId)
    {
        if (!_options.TracePageBuilder)
            return null;

        var activity = OpenTelemetryExtensions.BaselineActivitySource.StartActivity(
            "PageBuilder.Render",
            ActivityKind.Internal);

        activity?.SetTag("baseline.component_type", componentType);
        activity?.SetTag("baseline.component_id", componentId);

        return activity;
    }

    public void RecordContentQuery(string contentType, int resultCount, TimeSpan duration, bool fromCache)
    {
        BaselineMetrics.ContentQueryCounter.Add(1,
            new KeyValuePair<string, object?>("content_type", contentType),
            new KeyValuePair<string, object?>("from_cache", fromCache));

        BaselineMetrics.ContentQueryDuration.Record(duration.TotalMilliseconds,
            new KeyValuePair<string, object?>("content_type", contentType));

        BaselineMetrics.ContentQueryResultCount.Record(resultCount,
            new KeyValuePair<string, object?>("content_type", contentType));
    }

    public void RecordCacheAccess(string cacheKey, bool isHit)
    {
        var status = isHit ? "hit" : "miss";
        BaselineMetrics.CacheAccessCounter.Add(1,
            new KeyValuePair<string, object?>("status", status));
    }
}

/// <summary>
/// Baseline metrics for OpenTelemetry.
/// </summary>
public static class BaselineMetrics
{
    public const string MeterName = "Baseline.Core";

    private static readonly System.Diagnostics.Metrics.Meter Meter = new(MeterName, "3.0.0");

    public static readonly System.Diagnostics.Metrics.Counter<long> ContentQueryCounter =
        Meter.CreateCounter<long>("baseline.content_query.count", "queries", "Number of content queries executed");

    public static readonly System.Diagnostics.Metrics.Histogram<double> ContentQueryDuration =
        Meter.CreateHistogram<double>("baseline.content_query.duration", "ms", "Content query duration in milliseconds");

    public static readonly System.Diagnostics.Metrics.Histogram<int> ContentQueryResultCount =
        Meter.CreateHistogram<int>("baseline.content_query.result_count", "items", "Number of items returned by content queries");

    public static readonly System.Diagnostics.Metrics.Counter<long> CacheAccessCounter =
        Meter.CreateCounter<long>("baseline.cache.access", "accesses", "Number of cache accesses");

    public static readonly System.Diagnostics.Metrics.Counter<long> PageBuilderRenderCounter =
        Meter.CreateCounter<long>("baseline.pagebuilder.render", "renders", "Number of Page Builder component renders");
}
