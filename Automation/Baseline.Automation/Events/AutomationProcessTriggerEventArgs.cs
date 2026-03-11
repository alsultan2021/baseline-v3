namespace Baseline.Automation.Events;

/// <summary>
/// Event arguments for automation process trigger events.
/// Raised when a trigger fires and may start a process for a contact.
/// Maps to CMS.Automation.Internal.AutomationProcessTriggerEventArgs.
/// </summary>
public class AutomationProcessTriggerEventArgs : EventArgs
{
    /// <summary>ID of the automation process being triggered.</summary>
    public Guid ProcessId { get; set; }

    /// <summary>Contact ID for which the trigger fired.</summary>
    public int ContactId { get; set; }

    /// <summary>Type of trigger that fired.</summary>
    public AutomationTriggerType TriggerType { get; set; }

    /// <summary>Trigger-specific event data.</summary>
    public TriggerEventData? EventData { get; set; }

    /// <summary>Whether this trigger event should be cancelled.</summary>
    public bool Cancel { get; set; }

    /// <summary>Whether the contact was successfully started in the process.</summary>
    public bool Started { get; set; }

    /// <summary>Reason for not starting (if applicable).</summary>
    public string? SkipReason { get; set; }

    /// <summary>Timestamp of the trigger event.</summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}
