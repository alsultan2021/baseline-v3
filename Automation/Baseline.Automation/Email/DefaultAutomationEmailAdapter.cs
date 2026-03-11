using Microsoft.Extensions.Logging;

namespace Baseline.Automation.Email;

/// <summary>
/// Default no-op email adapter. Replace with a real implementation to enable email sending.
/// Maps to CMS.Automation.Internal.DefaultAutomationEmailAdapter.
/// </summary>
public class DefaultAutomationEmailAdapter(ILogger<DefaultAutomationEmailAdapter> logger) : IAutomationEmailAdapter
{
    public Task<bool> SendAsync(AutomationEmailMessage message)
    {
        logger.LogWarning(
            "DefaultAutomationEmailAdapter: Email sending not configured. " +
            "Register a real IAutomationEmailAdapter to enable automation emails. " +
            "Template={Template}, ContactId={ContactId}, ProcessId={ProcessId}",
            message.TemplateName, message.ContactId, message.ProcessId);

        return Task.FromResult(false);
    }

    public Task<string?> ResolveContactEmailAsync(int contactId)
    {
        logger.LogDebug("DefaultAutomationEmailAdapter: Cannot resolve email for contact {ContactId}", contactId);
        return Task.FromResult<string?>(null);
    }

    public Task<string?> ResolveTemplateAsync(string templateName, Dictionary<string, string> data)
    {
        logger.LogDebug("DefaultAutomationEmailAdapter: Cannot resolve template {TemplateName}", templateName);
        return Task.FromResult<string?>(null);
    }
}
