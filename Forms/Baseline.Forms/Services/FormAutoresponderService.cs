using Baseline.Forms.Configuration;
using Baseline.Forms.Interfaces;
using CMS.EmailEngine;
using CMS.Notifications;
using CMS.OnlineForms;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baseline.Forms.Services;

/// <summary>
/// Default implementation of form autoresponder and notification emails.
/// Prefers <see cref="INotificationEmailMessageProvider"/> for branded templates
/// and falls back to <see cref="IEmailService"/> with inline HTML.
/// </summary>
public sealed class FormAutoresponderService(
    IFormRetrievalService formRetrievalService,
    IEmailService emailService,
    INotificationEmailMessageProvider notificationEmailMessageProvider,
    IOptions<BaselineFormsOptions> options,
    ILogger<FormAutoresponderService> logger) : IFormAutoresponderService
{
    private readonly IFormRetrievalService _formRetrievalService = formRetrievalService;
    private readonly IEmailService _emailService = emailService;
    private readonly INotificationEmailMessageProvider _notificationEmailMessageProvider = notificationEmailMessageProvider;
    private readonly BaselineFormsOptions _options = options.Value;
    private readonly ILogger<FormAutoresponderService> _logger = logger;

    /// <inheritdoc />
    public async Task SendAutoresponderAsync(
        string formCodeName,
        string recipientEmail,
        IDictionary<string, object?> formData)
    {
        if (!_options.EnableAutoresponders)
        {
            _logger.LogDebug("Autoresponders disabled, skipping for form {Form}", formCodeName);
            return;
        }

        if (string.IsNullOrWhiteSpace(recipientEmail))
        {
            _logger.LogWarning("No recipient email for autoresponder on form {Form}", formCodeName);
            return;
        }

        try
        {
            var form = await _formRetrievalService.GetFormAsync(formCodeName);
            if (form is null)
            {
                _logger.LogWarning("Form {Form} not found for autoresponder", formCodeName);
                return;
            }

            var fromEmail = _options.DefaultFromEmail;
            if (string.IsNullOrWhiteSpace(fromEmail))
            {
                _logger.LogWarning("No DefaultFromEmail configured — cannot send autoresponder for form {Form}", formCodeName);
                return;
            }

            // Try notification template first (e.g. "FormAutoresponder_{formCodeName}")
            if (await TrySendViaNotificationTemplateAsync(
                    $"FormAutoresponder_{formCodeName}", recipientEmail, form, formData))
            {
                _logger.LogInformation("Branded autoresponder sent to {Email} for form {Form}", recipientEmail, formCodeName);
                return;
            }

            // Fall back to inline HTML
            var subject = $"Thank you for your submission — {form.FormDisplayName}";
            var body = BuildAutoresponderBody(form, formData);

            await _emailService.SendEmail(new EmailMessage
            {
                From = fromEmail,
                Recipients = recipientEmail,
                Subject = subject,
                Body = body,
                EmailFormat = EmailFormatEnum.Both
            });

            _logger.LogInformation("Autoresponder sent to {Email} for form {Form}", recipientEmail, formCodeName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send autoresponder for form {Form} to {Email}", formCodeName, recipientEmail);
        }
    }

    /// <inheritdoc />
    public async Task SendNotificationAsync(
        string formCodeName,
        IDictionary<string, object?> formData)
    {
        if (!_options.EnableNotifications)
        {
            _logger.LogDebug("Notifications disabled, skipping for form {Form}", formCodeName);
            return;
        }

        try
        {
            var form = await _formRetrievalService.GetFormAsync(formCodeName);
            if (form is null)
            {
                _logger.LogWarning("Form {Form} not found for notification", formCodeName);
                return;
            }

            var fromEmail = _options.DefaultFromEmail ?? "noreply@example.com";
            var notificationEmail = _options.NotificationRecipientEmail;

            if (string.IsNullOrWhiteSpace(notificationEmail))
            {
                _logger.LogDebug("No NotificationRecipientEmail configured for form {Form}", formCodeName);
                return;
            }

            var subject = $"New submission: {form.FormDisplayName}";
            var body = BuildNotificationBody(form, formData);

            await _emailService.SendEmail(new EmailMessage
            {
                From = fromEmail,
                Recipients = notificationEmail,
                Subject = subject,
                Body = body,
                EmailFormat = EmailFormatEnum.Both
            });

            _logger.LogInformation("Notification sent to {Recipients} for form {Form}", notificationEmail, formCodeName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification for form {Form}", formCodeName);
        }
    }

    /// <summary>
    /// Attempts to send email via a notification template. Returns true if successful.
    /// </summary>
    private async Task<bool> TrySendViaNotificationTemplateAsync(
        string templateName,
        string recipientEmail,
        BizFormInfo form,
        IDictionary<string, object?> formData)
    {
        try
        {
            var placeholders = new FormSubmissionPlaceholders(templateName, form.FormDisplayName, formData);
            var message = await _notificationEmailMessageProvider.CreateEmailMessage(
                placeholders.NotificationEmailName, 0, placeholders);
            return message != null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Notification template {Template} not available, falling back to inline HTML", templateName);
            return false;
        }
    }

    private static string BuildAutoresponderBody(BizFormInfo form, IDictionary<string, object?> formData)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<h2>Thank you for your submission</h2>");
        sb.AppendLine($"<p>We received your submission for <strong>{form.FormDisplayName}</strong>.</p>");
        sb.AppendLine("<p>Here is a summary of what you submitted:</p>");
        sb.AppendLine("<table style='border-collapse:collapse;width:100%'>");

        foreach (var (key, value) in formData)
        {
            if (key.StartsWith("Form", StringComparison.OrdinalIgnoreCase))
            {
                continue; // Skip system fields
            }

            sb.AppendLine($"<tr><td style='padding:4px 8px;border:1px solid #ddd'><strong>{key}</strong></td>");
            sb.AppendLine($"<td style='padding:4px 8px;border:1px solid #ddd'>{value}</td></tr>");
        }

        sb.AppendLine("</table>");
        return sb.ToString();
    }

    private static string BuildNotificationBody(BizFormInfo form, IDictionary<string, object?> formData)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"<h2>New submission: {form.FormDisplayName}</h2>");
        sb.AppendLine($"<p>Submitted at {DateTime.UtcNow:u}.</p>");
        sb.AppendLine("<table style='border-collapse:collapse;width:100%'>");

        foreach (var (key, value) in formData)
        {
            sb.AppendLine($"<tr><td style='padding:4px 8px;border:1px solid #ddd'><strong>{key}</strong></td>");
            sb.AppendLine($"<td style='padding:4px 8px;border:1px solid #ddd'>{value}</td></tr>");
        }

        sb.AppendLine("</table>");
        return sb.ToString();
    }
}

/// <summary>
/// Bridges form submission data to the <see cref="INotificationEmailPlaceholdersByCodeName"/>
/// interface so notification templates can use form field values as placeholders.
/// </summary>
public class FormSubmissionPlaceholders(
    string templateName,
    string formDisplayName,
    IDictionary<string, object?> formData) : INotificationEmailPlaceholdersByCodeName
{
    public string NotificationEmailName => templateName;

    public Dictionary<string, Func<string>> GetPlaceholders()
    {
        var result = new Dictionary<string, Func<string>>
        {
            ["FormName"] = () => formDisplayName,
            ["SubmittedAt"] = () => DateTime.UtcNow.ToString("u")
        };

        foreach (var (key, value) in formData)
        {
            if (!key.StartsWith("Form", StringComparison.OrdinalIgnoreCase))
            {
                result[key] = () => value?.ToString() ?? "";
            }
        }

        return result;
    }
}
