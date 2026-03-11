using Microsoft.Extensions.Logging;

namespace Baseline.Automation.Actions;

/// <summary>
/// Base class for automation actions providing common logging and parameter handling.
/// Maps to CMS.AutomationEngine.Internal.BaseWorkflowAction.
/// </summary>
public abstract class BaseAutomationAction(ILogger logger) : IAutomationAction
{
    protected ILogger Logger { get; } = logger;

    /// <summary>
    /// Template method: validates parameters, executes the action, handles errors.
    /// </summary>
    public async Task<AutomationActionResult> ExecuteAsync(int contactId, Guid processId, Guid stepId, string? parameters)
    {
        try
        {
            Logger.LogDebug("Executing action {ActionType} for contact {ContactId} in process {ProcessId} step {StepId}",
                GetType().Name, contactId, processId, stepId);

            var validationError = ValidateParameters(parameters);
            if (validationError is not null)
            {
                Logger.LogWarning("Action {ActionType} parameter validation failed: {Error}", GetType().Name, validationError);
                return AutomationActionResult.Fail(validationError);
            }

            var result = await ExecuteCoreAsync(contactId, processId, stepId, parameters);

            Logger.LogDebug("Action {ActionType} completed with success={Success} for contact {ContactId}",
                GetType().Name, result.Success, contactId);

            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Action {ActionType} failed for contact {ContactId} in process {ProcessId}",
                GetType().Name, contactId, processId);

            return AutomationActionResult.Fail(ex.Message);
        }
    }

    /// <summary>Override to implement the action logic.</summary>
    protected abstract Task<AutomationActionResult> ExecuteCoreAsync(
        int contactId, Guid processId, Guid stepId, string? parameters);

    /// <summary>Override to validate action parameters. Return null if valid.</summary>
    protected virtual string? ValidateParameters(string? parameters) => null;
}
