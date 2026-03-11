using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Baseline.Automation.Scheduling;

/// <summary>
/// Background task that periodically recalculates automation step statistics.
/// Maps to CMS.Automation.Internal.AutomationProcessStatisticsCalculationTask.
/// </summary>
public class AutomationStatisticsCalculationTask(
    IServiceProvider serviceProvider,
    ILogger<AutomationStatisticsCalculationTask> logger) : BackgroundService
{
    private static readonly TimeSpan CalculationInterval = TimeSpan.FromMinutes(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("AutomationStatisticsCalculationTask started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(CalculationInterval, stoppingToken);
                await CalculateStatisticsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error calculating automation statistics");
            }
        }
    }

    private async Task CalculateStatisticsAsync(CancellationToken cancellationToken)
    {
        using var scope = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.CreateScope(serviceProvider);
        var calculator = scope.ServiceProvider.GetService<IAutomationProcessStatisticsCalculator>();

        if (calculator is null)
        {
            return;
        }

        await calculator.CalculateAsync(cancellationToken);
    }
}
