using Microsoft.Extensions.Logging;

namespace Baseline.Automation.Services;

/// <summary>
/// Default implementation of <see cref="IAutomationProcessService"/>.
/// Manages automation process definitions (CRUD).
/// </summary>
public class AutomationProcessService(
    IProcessRepository processRepository,
    ILogger<AutomationProcessService> logger) : IAutomationProcessService
{
    /// <inheritdoc/>
    public async Task<AutomationProcess?> GetProcessAsync(Guid processId) =>
        await processRepository.GetByIdAsync(processId);

    /// <inheritdoc/>
    public async Task<IEnumerable<AutomationProcess>> GetProcessesAsync(bool? enabledOnly = null) =>
        await processRepository.GetAllAsync(enabledOnly);

    /// <inheritdoc/>
    public async Task<IEnumerable<AutomationProcess>> GetProcessesByTriggerAsync(AutomationTriggerType triggerType) =>
        await processRepository.GetByTriggerTypeAsync(triggerType);

    /// <inheritdoc/>
    public async Task<AutomationProcess> CreateProcessAsync(AutomationProcess process)
    {
        ArgumentNullException.ThrowIfNull(process);

        logger.LogInformation("Creating automation process: {ProcessName}", process.Name);

        var created = await processRepository.SaveAsync(process with
        {
            CreatedAt = DateTimeOffset.UtcNow,
            IsEnabled = false // New processes start disabled
        });

        return created;
    }

    /// <inheritdoc/>
    public async Task<AutomationProcess> UpdateProcessAsync(AutomationProcess process)
    {
        ArgumentNullException.ThrowIfNull(process);

        logger.LogInformation("Updating automation process: {ProcessName}", process.Name);

        return await processRepository.SaveAsync(process with
        {
            ModifiedAt = DateTimeOffset.UtcNow
        });
    }

    /// <inheritdoc/>
    public async Task EnableProcessAsync(Guid processId)
    {
        var process = await processRepository.GetByIdAsync(processId)
            ?? throw new InvalidOperationException($"Process {processId} not found");

        if (process.Steps.Count == 0)
        {
            throw new InvalidOperationException("Cannot enable a process with no steps");
        }

        await processRepository.SaveAsync(process with
        {
            IsEnabled = true,
            ModifiedAt = DateTimeOffset.UtcNow
        });

        logger.LogInformation("Enabled automation process: {ProcessName}", process.Name);
    }

    /// <inheritdoc/>
    public async Task DisableProcessAsync(Guid processId)
    {
        var process = await processRepository.GetByIdAsync(processId)
            ?? throw new InvalidOperationException($"Process {processId} not found");

        await processRepository.SaveAsync(process with
        {
            IsEnabled = false,
            ModifiedAt = DateTimeOffset.UtcNow
        });

        logger.LogInformation("Disabled automation process: {ProcessName} (contacts in progress will continue)", process.Name);
    }

    /// <inheritdoc/>
    public async Task DeleteProcessAsync(Guid processId)
    {
        logger.LogWarning("Deleting automation process {ProcessId} — all contact history will be cleared", processId);
        await processRepository.DeleteAsync(processId);
    }
}
