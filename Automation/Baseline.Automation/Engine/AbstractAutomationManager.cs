using Baseline.Automation.Events;
using Microsoft.Extensions.Logging;

namespace Baseline.Automation.Engine;

/// <summary>
/// Abstract base class for automation workflow managers.
/// Provides common step navigation, event raising, and state management.
/// Maps to CMS.AutomationEngine.Internal.AbstractWorkflowManager and CMS.Automation.Internal.AbstractAutomationManager.
/// </summary>
public abstract class AbstractAutomationManager
{
    /// <summary>Events instance for raising lifecycle events.</summary>
    protected AutomationEvents Events { get; } = AutomationEvents.Instance;

    /// <summary>
    /// Moves a contact to the next step, raising transitioning/entered events.
    /// </summary>
    protected async Task<bool> MoveToStepAsync(
        AutomationProcessItem processItem,
        Guid targetStepId,
        ILogger logger)
    {
        var transitionArgs = new AutomationEventArgs
        {
            ProcessId = processItem.ProcessId,
            ContactId = processItem.ContactId,
            CurrentStepId = processItem.CurrentStepId,
            PreviousStepId = processItem.CurrentStepId
        };

        // Allow cancellation
        if (!Events.OnStepTransitioning(transitionArgs))
        {
            logger.LogDebug("Step transition cancelled for contact {ContactId} in process {ProcessId}",
                processItem.ContactId, processItem.ProcessId);
            return false;
        }

        processItem.CurrentStepId = targetStepId;
        processItem.StepEnteredAt = DateTimeOffset.UtcNow;

        var enteredArgs = new AutomationEventArgs
        {
            ProcessId = processItem.ProcessId,
            ContactId = processItem.ContactId,
            CurrentStepId = targetStepId,
            PreviousStepId = transitionArgs.CurrentStepId
        };

        Events.OnStepEntered(enteredArgs);

        await OnStepEnteredAsync(processItem, logger);
        return true;
    }

    /// <summary>
    /// Starts a contact in an automation process at the first step.
    /// </summary>
    protected async Task StartProcessAsync(AutomationProcessItem processItem, ILogger logger)
    {
        Events.OnProcessStarted(new AutomationEventArgs
        {
            ProcessId = processItem.ProcessId,
            ContactId = processItem.ContactId,
            CurrentStepId = processItem.CurrentStepId
        });

        await OnProcessStartedAsync(processItem, logger);
    }

    /// <summary>
    /// Finishes a contact's run through an automation process.
    /// </summary>
    protected async Task FinishProcessAsync(AutomationProcessItem processItem, ILogger logger)
    {
        processItem.IsFinished = true;
        processItem.FinishedAt = DateTimeOffset.UtcNow;

        Events.OnProcessFinished(new AutomationEventArgs
        {
            ProcessId = processItem.ProcessId,
            ContactId = processItem.ContactId,
            CurrentStepId = processItem.CurrentStepId
        });

        await OnProcessFinishedAsync(processItem, logger);
    }

    /// <summary>
    /// Handles a timeout on the current step.
    /// </summary>
    protected async Task HandleTimeoutAsync(AutomationProcessItem processItem, Guid timeoutTargetStepId, ILogger logger)
    {
        Events.OnStepTimedOut(new AutomationEventArgs
        {
            ProcessId = processItem.ProcessId,
            ContactId = processItem.ContactId,
            CurrentStepId = processItem.CurrentStepId
        });

        await MoveToStepAsync(processItem, timeoutTargetStepId, logger);
    }

    /// <summary>Override to handle post-step-entry logic.</summary>
    protected virtual Task OnStepEnteredAsync(AutomationProcessItem processItem, ILogger logger) =>
        Task.CompletedTask;

    /// <summary>Override to handle post-process-start logic.</summary>
    protected virtual Task OnProcessStartedAsync(AutomationProcessItem processItem, ILogger logger) =>
        Task.CompletedTask;

    /// <summary>Override to handle post-process-finish logic.</summary>
    protected virtual Task OnProcessFinishedAsync(AutomationProcessItem processItem, ILogger logger) =>
        Task.CompletedTask;
}
