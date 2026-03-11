using Microsoft.Extensions.Logging;

namespace Baseline.Automation.Actions;

/// <summary>
/// Action that starts another automation process for the same contact.
/// Maps to CMS.Automation.Internal.StartProcessAction.
/// </summary>
public class StartProcessAction(
    IAutomationEngine automationEngine,
    ILogger<StartProcessAction> logger) : BaseAutomationAction(logger)
{
    protected override async Task<AutomationActionResult> ExecuteCoreAsync(
        int contactId, Guid processId, Guid stepId, string? parameters)
    {
        if (string.IsNullOrEmpty(parameters))
        {
            return AutomationActionResult.Fail("Start process action requires target process ID");
        }

        var config = System.Text.Json.JsonSerializer.Deserialize<StartProcessParameters>(parameters);
        if (config is null || config.TargetProcessId == Guid.Empty)
        {
            return AutomationActionResult.Fail("Invalid target process ID");
        }

        var triggerData = new TriggerEventData
        {
            ContactId = contactId,
            TriggerType = AutomationTriggerType.Manual,
            Data = System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, string>
            {
                ["SourceProcessId"] = processId.ToString(),
                ["SourceStepId"] = stepId.ToString()
            })
        };

        var started = await automationEngine.FireTriggerAsync(triggerData);

        return started > 0
            ? AutomationActionResult.Ok()
            : AutomationActionResult.Fail($"Failed to start process {config.TargetProcessId}");
    }

    protected override string? ValidateParameters(string? parameters) =>
        string.IsNullOrEmpty(parameters) ? "Target process ID is required" : null;
}

/// <summary>Parameters for the start process action.</summary>
public record StartProcessParameters
{
    public Guid TargetProcessId { get; init; }
}
