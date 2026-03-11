using CMS.DataEngine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Baseline.Core.HealthChecks;

/// <summary>
/// Health check that verifies the Xperience database connection is working.
/// Uses ObjectQuery for lightweight DB connectivity test.
/// </summary>
public sealed class XperienceDatabaseHealthCheck : IHealthCheck
{
    private const string HealthyMessage = "Xperience database connection is healthy.";
    private const string UnhealthyMessage = "Xperience database connection failed.";

    /// <inheritdoc/>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // ObjectQuery is sync-only; Task.Run is acceptable for infrequent health checks
            var result = await Task.Run(() =>
                new ObjectQuery("cms.settingskey")
                    .TopN(1)
                    .Column("KeyName")
                    .GetEnumerableTypedResult()
                    .Any(),
                cancellationToken);

            return result
                ? HealthCheckResult.Healthy(HealthyMessage)
                : HealthCheckResult.Degraded("Database query returned no results.");
        }
        catch (OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy("Health check was cancelled.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(UnhealthyMessage, exception: ex);
        }
    }
}

/// <summary>
/// Health check that verifies the Xperience cache is functioning.
/// </summary>
public sealed class XperienceCacheHealthCheck : IHealthCheck
{
    private const string TestCacheKey = "Baseline.HealthCheck.CacheTest";

    /// <inheritdoc/>
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var testValue = Guid.NewGuid().ToString();

            // Use cache settings for adding
            var cacheSettings = new CMS.Helpers.CacheSettings(10, TestCacheKey);
            CMS.Helpers.CacheHelper.Cache(() => testValue, cacheSettings);

            // Test cache read
            CMS.Helpers.CacheHelper.TryGetItem<string>(TestCacheKey, out var retrieved);

            // Test cache remove
            CMS.Helpers.CacheHelper.Remove(TestCacheKey);

            if (retrieved == testValue)
            {
                return Task.FromResult(HealthCheckResult.Healthy("Xperience cache is functioning correctly."));
            }

            return Task.FromResult(HealthCheckResult.Degraded("Cache read/write mismatch."));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Xperience cache check failed.", exception: ex));
        }
    }
}

/// <summary>
/// Health check that verifies Xperience background services are running.
/// Uses ObjectQuery for lightweight scheduled task check.
/// </summary>
public sealed class XperienceSchedulerHealthCheck : IHealthCheck
{
    /// <inheritdoc/>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var hasScheduledTasks = await Task.Run(() =>
                new ObjectQuery("cms.scheduledtask")
                    .TopN(1)
                    .Column("TaskName")
                    .GetEnumerableTypedResult()
                    .Any(),
                cancellationToken);

            return HealthCheckResult.Healthy(
                hasScheduledTasks
                    ? "Xperience scheduler has configured tasks."
                    : "No scheduled tasks configured.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Xperience scheduler check failed.", exception: ex);
        }
    }
}

/// <summary>
/// Extension methods for adding Xperience health checks.
/// </summary>
public static class XperienceHealthCheckExtensions
{
    /// <summary>
    /// Adds Xperience database health check.
    /// </summary>
    public static IHealthChecksBuilder AddXperienceDatabase(
        this IHealthChecksBuilder builder,
        string? name = null,
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null,
        TimeSpan? timeout = null)
    {
        return builder.AddCheck<XperienceDatabaseHealthCheck>(
            name ?? "xperience-database",
            failureStatus,
            tags ?? ["xperience", "database", "ready"],
            timeout);
    }

    /// <summary>
    /// Adds Xperience cache health check.
    /// </summary>
    public static IHealthChecksBuilder AddXperienceCache(
        this IHealthChecksBuilder builder,
        string? name = null,
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null,
        TimeSpan? timeout = null)
    {
        return builder.AddCheck<XperienceCacheHealthCheck>(
            name ?? "xperience-cache",
            failureStatus ?? HealthStatus.Degraded,
            tags ?? ["xperience", "cache"],
            timeout);
    }

    /// <summary>
    /// Adds Xperience scheduler health check.
    /// </summary>
    public static IHealthChecksBuilder AddXperienceScheduler(
        this IHealthChecksBuilder builder,
        string? name = null,
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null,
        TimeSpan? timeout = null)
    {
        return builder.AddCheck<XperienceSchedulerHealthCheck>(
            name ?? "xperience-scheduler",
            failureStatus ?? HealthStatus.Degraded,
            tags ?? ["xperience", "scheduler"],
            timeout);
    }

    /// <summary>
    /// Adds all Xperience health checks.
    /// </summary>
    public static IHealthChecksBuilder AddXperience(this IHealthChecksBuilder builder)
    {
        return builder
            .AddXperienceDatabase()
            .AddXperienceCache()
            .AddXperienceScheduler();
    }
}
