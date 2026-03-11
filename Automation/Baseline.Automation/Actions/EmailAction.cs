using Baseline.Automation.Email;
using Microsoft.Extensions.Logging;

namespace Baseline.Automation.Actions;

/// <summary>
/// Action that sends an email as part of an automation step.
/// Maps to CMS.Automation.Internal.EmailAction.
/// </summary>
public class EmailAction(
    IAutomationEmailAdapter emailAdapter,
    ILogger<EmailAction> logger) : BaseAutomationAction(logger)
{
    protected override async Task<AutomationActionResult> ExecuteCoreAsync(
        int contactId, Guid processId, Guid stepId, string? parameters)
    {
        if (string.IsNullOrEmpty(parameters))
        {
            return AutomationActionResult.Fail("Email action requires parameters");
        }

        var emailConfig = System.Text.Json.JsonSerializer.Deserialize<EmailActionParameters>(parameters);
        if (emailConfig is null)
        {
            return AutomationActionResult.Fail("Invalid email action parameters");
        }

        var email = new AutomationEmailMessage
        {
            ContactId = contactId,
            ProcessId = processId,
            StepId = stepId,
            TemplateName = emailConfig.TemplateName,
            Subject = emailConfig.Subject,
            CustomData = emailConfig.CustomData
        };

        var sent = await emailAdapter.SendAsync(email);
        return sent ? AutomationActionResult.Ok() : AutomationActionResult.Fail("Email sending failed");
    }

    protected override string? ValidateParameters(string? parameters) =>
        string.IsNullOrEmpty(parameters) ? "Email parameters are required" : null;
}

/// <summary>Parameters for the email action.</summary>
public record EmailActionParameters
{
    public string TemplateName { get; init; } = "";
    public string? Subject { get; init; }
    public Dictionary<string, string> CustomData { get; init; } = new();
}
