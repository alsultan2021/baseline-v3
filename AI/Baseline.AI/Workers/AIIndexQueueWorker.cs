using Baseline.AI.Indexing;

using CMS.Base;
using CMS.DataEngine;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Baseline.AI.Workers;

/// <summary>
/// Background service that processes the AI indexing queue.
/// Runs periodically to process queued indexing operations.
/// Uses IServiceProvider for lazy resolution to avoid blocking startup.
/// </summary>
public class AIIndexQueueWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AIIndexQueueWorker> _logger;
    private readonly TimeSpan _interval;

    public AIIndexQueueWorker(
        IServiceProvider serviceProvider,
        ILogger<AIIndexQueueWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _interval = TimeSpan.FromMinutes(1);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogDebug("AI Index Queue Worker started");

        try
        {
            // Wait for app to fully initialize (installers to complete)
            await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Clear inherited thread context and use a fresh DB connection
                // to prevent "BeginExecuteReader requires an open and available Connection" errors
                // See: https://docs.kentico.com/documentation/developers-and-admins/customization/integrate-custom-code
                ContextUtils.ResetCurrent();

                // Resolve IAIIndexManager lazily to avoid blocking startup
                using var scope = _serviceProvider.CreateScope();
                var indexManager = scope.ServiceProvider.GetService<IAIIndexManager>();

                if (indexManager is null)
                {
                    _logger.LogDebug("IAIIndexManager not available yet, waiting...");
                    await Task.Delay(_interval, stoppingToken);
                    continue;
                }

                // Retry once on transient SQL errors (stale pooled connection)
                int processed = 0;
                for (var attempt = 0; attempt < 2; attempt++)
                {
                    try
                    {
                        using (new CMSConnectionScope(true))
                        {
                            processed = await indexManager.ProcessQueueAsync(
                                knowledgeBaseId: null,
                                batchSize: 100,
                                cancellationToken: stoppingToken);
                        }

                        break; // success
                    }
                    catch (Exception ex) when (attempt == 0 && IsTransientSqlError(ex))
                    {
                        _logger.LogWarning("[AIQueue] Transient SQL error, retrying: {Msg}", ex.Message);
                        ContextUtils.ResetCurrent();
                        await Task.Delay(500, stoppingToken);
                    }
                }

                if (processed > 0)
                {
                    _logger.LogInformation("Processed {Count} queue items", processed);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error processing AI index queue");
            }

            try
            {
                await Task.Delay(_interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("AI Index Queue Worker stopped");
    }

    /// <summary>
    /// Detects transient SQL errors (stale pooled connections, network blips).
    /// </summary>
    private static bool IsTransientSqlError(Exception ex)
    {
        var current = ex;
        while (current is not null)
        {
            var msg = current.Message;
            if (msg.Contains("Physical connection is not usable", StringComparison.OrdinalIgnoreCase)
                || msg.Contains("transport-level error", StringComparison.OrdinalIgnoreCase)
                || msg.Contains("connection is broken", StringComparison.OrdinalIgnoreCase)
                || msg.Contains("requires an open and available Connection", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            current = current.InnerException;
        }

        return false;
    }
}
