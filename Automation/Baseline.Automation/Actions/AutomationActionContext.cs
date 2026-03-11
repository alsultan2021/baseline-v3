namespace Baseline.Automation.Actions;

/// <summary>
/// Provides contextual information about the currently executing automation action.
/// Thread-scoped context using AsyncLocal for safe concurrent access.
/// Maps to CMS.Automation.Internal.AutomationActionContext / CMS.AutomationEngine.Internal.WorkflowActionContext.
/// </summary>
public class AutomationActionContext : IDisposable
{
    private static readonly AsyncLocal<AutomationActionContext?> _current = new();

    /// <summary>Gets the current action context.</summary>
    public static AutomationActionContext? Current => _current.Value;

    /// <summary>ID of the automation process.</summary>
    public Guid ProcessId { get; init; }

    /// <summary>Contact ID being processed.</summary>
    public int ContactId { get; init; }

    /// <summary>Current step ID.</summary>
    public Guid StepId { get; init; }

    /// <summary>Step type being executed.</summary>
    public AutomationStepType StepType { get; init; }

    /// <summary>Name of the action being executed.</summary>
    public string ActionName { get; init; } = "";

    /// <summary>Whether logging is enabled for this action context.</summary>
    public bool LoggingEnabled { get; init; } = true;

    /// <summary>Whether the action should send notifications.</summary>
    public bool SendNotifications { get; init; } = true;

    /// <summary>Additional contextual data.</summary>
    public Dictionary<string, object?> ContextData { get; init; } = new();

    /// <summary>
    /// Creates a new context and sets it as current.
    /// Dispose to restore the previous context.
    /// </summary>
    public static AutomationActionContext Push(Guid processId, int contactId, Guid stepId, AutomationStepType stepType)
    {
        var context = new AutomationActionContext
        {
            ProcessId = processId,
            ContactId = contactId,
            StepId = stepId,
            StepType = stepType,
            _previousContext = _current.Value
        };
        _current.Value = context;
        return context;
    }

    private AutomationActionContext? _previousContext;

    public void Dispose() => _current.Value = _previousContext;
}
