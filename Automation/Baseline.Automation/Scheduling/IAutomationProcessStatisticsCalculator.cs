namespace Baseline.Automation.Scheduling;

/// <summary>
/// Interface for recalculating automation process step statistics.
/// Maps to CMS.Automation.Internal.IAutomationProcessStatisticsCalculator / AutomationProcessStatisticsCalculator.
/// </summary>
public interface IAutomationProcessStatisticsCalculator
{
    /// <summary>
    /// Recalculates statistics for all automation steps across all processes.
    /// </summary>
    Task CalculateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Recalculates statistics for a specific automation process.
    /// </summary>
    Task CalculateForProcessAsync(Guid processId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Increments the contacts-at-step counter when a contact enters a step.
    /// </summary>
    Task IncrementContactAtStepAsync(int stepId);

    /// <summary>
    /// Decrements the contacts-at-step counter when a contact leaves a step.
    /// </summary>
    Task DecrementContactAtStepAsync(int stepId);

    /// <summary>
    /// Records that a contact passed through a step with a given duration.
    /// </summary>
    Task RecordStepPassedAsync(int stepId, TimeSpan duration);
}
