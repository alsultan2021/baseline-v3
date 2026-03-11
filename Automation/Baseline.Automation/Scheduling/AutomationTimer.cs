using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Baseline.Automation.Configuration;

namespace Baseline.Automation.Scheduling;

/// <summary>
/// Background timer that checks for timed-out automation steps and advances contacts.
/// Maps to CMS.Automation.Internal.AutomationTimer.
/// </summary>
public class AutomationTimer(
    IServiceProvider serviceProvider,
    IOptions<AutomationOptions> options,
    ILogger<AutomationTimer> logger) : BackgroundService
{
    private readonly AutomationOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("AutomationTimer started, polling every {Interval}s", _options.PollingIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(_options.PollingIntervalSeconds), stoppingToken);
                await CheckTimeoutsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in AutomationTimer");
            }
        }

        logger.LogInformation("AutomationTimer stopped");
    }

    private async Task CheckTimeoutsAsync(CancellationToken cancellationToken)
    {
        using var scope = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.CreateScope(serviceProvider);
        var engine = scope.ServiceProvider.GetRequiredService<IAutomationEngine>();

        var processed = await engine.ProcessWaitingContactsAsync();

        if (processed > 0)
        {
            logger.LogDebug("AutomationTimer: Processed {Count} timed-out contacts", processed);
        }
    }
}
