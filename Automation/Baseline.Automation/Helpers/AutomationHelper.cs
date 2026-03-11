namespace Baseline.Automation.Helpers;

/// <summary>
/// Utility methods for common automation operations.
/// Maps to CMS.AutomationEngine.Internal.WorkflowHelper.
/// </summary>
public static class AutomationHelper
{
    /// <summary>
    /// Determines if a contact can enter a process based on recurrence settings.
    /// </summary>
    public static bool CanEnterProcess(ProcessRecurrence recurrence, int executionCount, bool isCurrentlyRunning) => recurrence switch
    {
        ProcessRecurrence.Always => true,
        ProcessRecurrence.OnlyOnce => executionCount == 0,
        ProcessRecurrence.IfNotAlreadyRunning => !isCurrentlyRunning,
        _ => false
    };

    /// <summary>
    /// Gets a step by ID from a process, or null if not found.
    /// </summary>
    public static AutomationStep? GetStep(AutomationProcess process, Guid stepId) =>
        process.Steps.FirstOrDefault(s => s.Id == stepId);

    /// <summary>
    /// Gets the next step after the given step (by order).
    /// </summary>
    public static AutomationStep? GetNextStep(AutomationProcess process, Guid currentStepId)
    {
        var currentStep = process.Steps.FirstOrDefault(s => s.Id == currentStepId);
        if (currentStep is null)
        {
            return null;
        }

        return process.Steps
            .Where(s => s.Order > currentStep.Order)
            .OrderBy(s => s.Order)
            .FirstOrDefault();
    }

    /// <summary>
    /// Gets the start step (trigger/first step) of a process.
    /// </summary>
    public static AutomationStep? GetStartStep(AutomationProcess process) =>
        process.Steps.OrderBy(s => s.Order).FirstOrDefault();

    /// <summary>
    /// Gets the finish step of a process.
    /// </summary>
    public static AutomationStep? GetFinishStep(AutomationProcess process) =>
        process.Steps.FirstOrDefault(s => s.StepType == AutomationStepType.Finish);

    /// <summary>
    /// Validates that a process has a valid structure (start + finish + at least one step).
    /// </summary>
    public static (bool IsValid, string? Error) ValidateProcessStructure(AutomationProcess process)
    {
        if (process.Steps.Count == 0)
        {
            return (false, "Process must have at least one step");
        }

        var startStep = GetStartStep(process);
        if (startStep is null)
        {
            return (false, "Process must have a start step");
        }

        var finishStep = GetFinishStep(process);
        if (finishStep is null)
        {
            return (false, "Process must have a finish step");
        }

        return (true, null);
    }

    /// <summary>
    /// Calculates the wait duration for a Wait step from its configuration.
    /// </summary>
    public static TimeSpan? CalculateWaitDuration(WaitStepConfig? config)
    {
        if (config is null)
        {
            return null;
        }

        if (config.UntilDate.HasValue)
        {
            var remaining = config.UntilDate.Value - DateTimeOffset.UtcNow;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }

        return config.IntervalMinutes.HasValue
            ? TimeSpan.FromMinutes(config.IntervalMinutes.Value)
            : null;
    }
}
