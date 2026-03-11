namespace Baseline.Automation.Events;

/// <summary>
/// Event arguments for automation process events (step transitions, completions, etc.).
/// Maps to CMS.Automation.Internal.AutomationEventArgs.
/// </summary>
public class AutomationEventArgs : EventArgs
{
    /// <summary>ID of the automation process.</summary>
    public Guid ProcessId { get; set; }

    /// <summary>Contact ID being processed.</summary>
    public int ContactId { get; set; }

    /// <summary>Current step ID.</summary>
    public Guid CurrentStepId { get; set; }

    /// <summary>Previous step ID (for transitions).</summary>
    public Guid? PreviousStepId { get; set; }

    /// <summary>Step type of the current step.</summary>
    public AutomationStepType StepType { get; set; }

    /// <summary>Whether the event was cancelled by a handler.</summary>
    public bool Cancel { get; set; }

    /// <summary>Optional contextual data.</summary>
    public Dictionary<string, object?> Data { get; set; } = new();

    /// <summary>Timestamp of the event.</summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Gets a typed value from the Data dictionary.</summary>
    public T? GetData<T>(string key) =>
        Data.TryGetValue(key, out var value) && value is T typed ? typed : default;

    /// <summary>Sets a value in the Data dictionary.</summary>
    public void SetData(string key, object? value) => Data[key] = value;
}
