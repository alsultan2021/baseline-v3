using Microsoft.Extensions.Logging;

namespace Baseline.Automation.Events;

/// <summary>
/// Central event registry for automation process lifecycle events.
/// Subscribers can hook into step transitions, process starts/completions, etc.
/// Maps to CMS.Automation.Internal.AutomationEvents.
/// </summary>
public class AutomationEvents
{
    /// <summary>Raised when a contact enters an automation process.</summary>
    public event EventHandler<AutomationEventArgs>? ProcessStarted;

    /// <summary>Raised when a contact finishes an automation process.</summary>
    public event EventHandler<AutomationEventArgs>? ProcessFinished;

    /// <summary>Raised when a contact is removed from a process.</summary>
    public event EventHandler<AutomationEventArgs>? ProcessAborted;

    /// <summary>Raised before a contact transitions to a new step.</summary>
    public event EventHandler<AutomationEventArgs>? StepTransitioning;

    /// <summary>Raised after a contact has entered a new step.</summary>
    public event EventHandler<AutomationEventArgs>? StepEntered;

    /// <summary>Raised when a step action executes.</summary>
    public event EventHandler<AutomationEventArgs>? ActionExecuting;

    /// <summary>Raised after a step action completes.</summary>
    public event EventHandler<AutomationEventArgs>? ActionExecuted;

    /// <summary>Raised when a step times out.</summary>
    public event EventHandler<AutomationEventArgs>? StepTimedOut;

    /// <summary>Raised when a condition is evaluated.</summary>
    public event EventHandler<AutomationEventArgs>? ConditionEvaluated;

    /// <summary>Raised when an error occurs during processing.</summary>
    public event EventHandler<AutomationEventArgs>? ProcessError;

    /// <summary>Singleton instance.</summary>
    public static AutomationEvents Instance { get; } = new();

    public void OnProcessStarted(AutomationEventArgs args) => ProcessStarted?.Invoke(this, args);
    public void OnProcessFinished(AutomationEventArgs args) => ProcessFinished?.Invoke(this, args);
    public void OnProcessAborted(AutomationEventArgs args) => ProcessAborted?.Invoke(this, args);

    public bool OnStepTransitioning(AutomationEventArgs args)
    {
        StepTransitioning?.Invoke(this, args);
        return !args.Cancel;
    }

    public void OnStepEntered(AutomationEventArgs args) => StepEntered?.Invoke(this, args);
    public void OnActionExecuting(AutomationEventArgs args) => ActionExecuting?.Invoke(this, args);
    public void OnActionExecuted(AutomationEventArgs args) => ActionExecuted?.Invoke(this, args);
    public void OnStepTimedOut(AutomationEventArgs args) => StepTimedOut?.Invoke(this, args);
    public void OnConditionEvaluated(AutomationEventArgs args) => ConditionEvaluated?.Invoke(this, args);
    public void OnProcessError(AutomationEventArgs args) => ProcessError?.Invoke(this, args);

    /// <summary>
    /// Logs all events to the provided logger for debugging.
    /// </summary>
    public void EnableLogging(ILogger logger)
    {
        ProcessStarted += (_, e) => logger.LogDebug("Automation: Process {ProcessId} started for contact {ContactId}", e.ProcessId, e.ContactId);
        ProcessFinished += (_, e) => logger.LogDebug("Automation: Process {ProcessId} finished for contact {ContactId}", e.ProcessId, e.ContactId);
        StepEntered += (_, e) => logger.LogDebug("Automation: Contact {ContactId} entered step {StepId} in process {ProcessId}", e.ContactId, e.CurrentStepId, e.ProcessId);
        ProcessError += (_, e) => logger.LogWarning("Automation: Error in process {ProcessId} for contact {ContactId} at step {StepId}", e.ProcessId, e.ContactId, e.CurrentStepId);
    }
}
