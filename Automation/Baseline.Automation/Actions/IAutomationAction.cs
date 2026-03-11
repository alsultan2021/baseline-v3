namespace Baseline.Automation.Actions;

/// <summary>
/// Interface for automation workflow actions that can be assigned to Action steps.
/// Actions are resolved by assembly/class name from the action definition.
/// Maps to CMS.AutomationEngine.Internal.IWorkflowAction.
/// </summary>
public interface IAutomationAction
{
    /// <summary>
    /// Executes the action with the given context and parameters.
    /// </summary>
    /// <param name="contactId">The contact being processed.</param>
    /// <param name="processId">The automation process ID.</param>
    /// <param name="stepId">The current step ID.</param>
    /// <param name="parameters">Action-specific parameters (XML or JSON).</param>
    /// <returns>The result of the action execution.</returns>
    Task<AutomationActionResult> ExecuteAsync(int contactId, Guid processId, Guid stepId, string? parameters);
}

/// <summary>
/// Result of an automation action execution.
/// </summary>
public record AutomationActionResult
{
    /// <summary>Whether the action completed successfully.</summary>
    public bool Success { get; init; } = true;

    /// <summary>Error message if the action failed.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>Action-specific result data.</summary>
    public Dictionary<string, object?> Data { get; init; } = new();

    /// <summary>Whether the process should move to a specific step instead of the default next step.</summary>
    public Guid? OverrideNextStepId { get; init; }

    /// <summary>Creates a success result.</summary>
    public static AutomationActionResult Ok() => new();

    /// <summary>Creates a failure result.</summary>
    public static AutomationActionResult Fail(string error) => new() { Success = false, ErrorMessage = error };
}
