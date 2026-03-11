namespace Baseline.Automation.Engine;

/// <summary>
/// Represents a contact's in-progress state within an automation process.
/// Used by the engine during step processing as a mutable working copy.
/// Maps to CMS.Automation.Internal.AutomationProcessItem.
/// </summary>
public class AutomationProcessItem
{
    /// <summary>ID of the automation process.</summary>
    public Guid ProcessId { get; set; }

    /// <summary>Contact ID being processed.</summary>
    public int ContactId { get; set; }

    /// <summary>Current step ID the contact is at.</summary>
    public Guid CurrentStepId { get; set; }

    /// <summary>When the contact entered the current step.</summary>
    public DateTimeOffset StepEnteredAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>When the contact started the process.</summary>
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Whether the process has finished for this contact.</summary>
    public bool IsFinished { get; set; }

    /// <summary>When the process finished.</summary>
    public DateTimeOffset? FinishedAt { get; set; }

    /// <summary>Trigger data that initiated the process (as JSON).</summary>
    public string? TriggerData { get; set; }

    /// <summary>Number of steps processed in the current execution cycle (loop guard).</summary>
    public int StepsProcessed { get; set; }

    /// <summary>Maximum steps allowed per execution cycle.</summary>
    public int MaxSteps { get; set; } = 100;

    /// <summary>Whether the step depth limit has been reached.</summary>
    public bool IsDepthLimitReached => StepsProcessed >= MaxSteps;

    /// <summary>
    /// Creates an AutomationProcessItem from a ProcessContactState.
    /// </summary>
    public static AutomationProcessItem FromState(ProcessContactState state) => new()
    {
        ProcessId = state.ProcessId,
        ContactId = state.ContactId,
        CurrentStepId = state.CurrentStepId,
        StepEnteredAt = state.StepEnteredAt,
        StartedAt = state.StartedAt,
        TriggerData = state.ContextData
    };
}
