using Baseline.Automation.Configuration;
using CMS.DataEngine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baseline.Automation.Services;

/// <summary>
/// Background service that periodically processes waiting contacts in automation processes.
/// Checks for expired wait steps and advances contacts to their next step.
/// </summary>
public class AutomationBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<AutomationOptions> options,
    ILogger<AutomationBackgroundService> logger) : BackgroundService
{
    private readonly AutomationOptions _options = options.Value;

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.EnableBackgroundProcessing)
        {
            logger.LogInformation("Automation background processing is disabled");
            return;
        }

        logger.LogInformation(
            "Automation background service started, polling every {Interval} seconds",
            _options.PollingIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessWaitingContactsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in automation background processing");
            }

            try
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(_options.PollingIntervalSeconds),
                    stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        logger.LogInformation("Automation background service stopped");
    }

    private async Task ProcessWaitingContactsAsync(CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        using var connectionScope = new CMSConnectionScope(true);
        var engine = scope.ServiceProvider.GetRequiredService<IAutomationEngine>();

        var advancedCount = await engine.ProcessWaitingContactsAsync();
        if (advancedCount > 0)
        {
            logger.LogInformation("Advanced {Count} waiting contacts", advancedCount);
        }
    }
}
