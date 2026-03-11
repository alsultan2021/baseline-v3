namespace Baseline.Automation.Email;

/// <summary>
/// Adapter interface for sending automation emails.
/// Decouples the automation engine from the email sending implementation.
/// Maps to CMS.Automation.Internal.IAutomationEmailAdapter.
/// </summary>
public interface IAutomationEmailAdapter
{
    /// <summary>
    /// Sends an automation email message.
    /// </summary>
    /// <param name="message">The email message to send.</param>
    /// <returns>True if the email was sent successfully.</returns>
    Task<bool> SendAsync(AutomationEmailMessage message);

    /// <summary>
    /// Resolves the recipient email address for a contact.
    /// </summary>
    /// <param name="contactId">The contact ID.</param>
    /// <returns>The email address, or null if not found.</returns>
    Task<string?> ResolveContactEmailAsync(int contactId);

    /// <summary>
    /// Resolves template content with macro data.
    /// </summary>
    /// <param name="templateName">The template name.</param>
    /// <param name="data">Macro replacement data.</param>
    /// <returns>Resolved template content.</returns>
    Task<string?> ResolveTemplateAsync(string templateName, Dictionary<string, string> data);
}
