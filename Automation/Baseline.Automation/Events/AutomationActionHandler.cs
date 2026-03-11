namespace Baseline.Automation.Events;

/// <summary>
/// Handler for automation action execution events.
/// Provides before/after hooks for action processing with cancellation support.
/// Maps to CMS.Automation.Internal.AutomationActionHandler.
/// </summary>
public class AutomationActionHandler
{
    /// <summary>Raised before an action executes.</summary>
    public event EventHandler<AutomationActionEventArgs>? Executing;

    /// <summary>Raised after an action completes.</summary>
    public event EventHandler<AutomationActionEventArgs>? Executed;

    /// <summary>Raises the Executing event. Returns false if cancelled.</summary>
    public bool OnExecuting(AutomationActionEventArgs args)
    {
        Executing?.Invoke(this, args);
        return !args.Cancel;
    }

    /// <summary>Raises the Executed event.</summary>
    public void OnExecuted(AutomationActionEventArgs args) =>
        Executed?.Invoke(this, args);
}

/// <summary>
/// Event arguments for automation action events.
/// </summary>
public class AutomationActionEventArgs : AutomationEventArgs
{
    /// <summary>Name of the action being executed.</summary>
    public string ActionName { get; set; } = "";

    /// <summary>Assembly name of the action class.</summary>
    public string ActionAssemblyName { get; set; } = "";

    /// <summary>Class name of the action implementation.</summary>
    public string ActionClassName { get; set; } = "";

    /// <summary>Action parameters.</summary>
    public string? ActionParameters { get; set; }

    /// <summary>Result of the action execution (populated after execution).</summary>
    public StepExecutionResult? Result { get; set; }
}
