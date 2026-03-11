using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Baseline.Automation.Actions;

/// <summary>
/// Flags a contact for follow-up by setting a custom field value.
/// </summary>
public class FlagContactActionExecutor(
    ILogger<FlagContactActionExecutor> logger) : IAutomationActionExecutor
{
    public AutomationStepType StepType => AutomationStepType.FlagContact;

    public Task<StepExecutionResult> ExecuteAsync(AutomationContext context)
    {
        var config = context.CurrentStep.GetConfiguration<FlagContactStepConfig>();
        if (config == null)
        {
            return Task.FromResult(StepExecutionResult.Failed("FlagContact step is missing configuration"));
        }

        try
        {
            var contact = CMS.ContactManagement.ContactInfo.Provider.Get(context.ContactId);
            if (contact == null)
            {
                return Task.FromResult(StepExecutionResult.Failed($"Contact {context.ContactId} not found"));
            }

            contact.SetValue(config.FlagFieldName, config.FlagValue);
            if (!string.IsNullOrEmpty(config.Note))
            {
                contact.SetValue("ContactNotes",
                    $"{contact.GetStringValue("ContactNotes", "")}\n[{DateTimeOffset.UtcNow:u}] Automation flag: {config.Note}".Trim());
            }
            CMS.ContactManagement.ContactInfo.Provider.Set(contact);

            logger.LogInformation(
                "Flagged contact {ContactId} with {FieldName}={Value}",
                context.ContactId, config.FlagFieldName, config.FlagValue);

            return Task.FromResult(StepExecutionResult.Succeeded());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error flagging contact {ContactId}", context.ContactId);
            return Task.FromResult(StepExecutionResult.Failed($"Flag contact failed: {ex.Message}"));
        }
    }
}

/// <summary>
/// Adds or removes a contact from a contact group.
/// </summary>
public class UpdateContactGroupActionExecutor(
    ILogger<UpdateContactGroupActionExecutor> logger) : IAutomationActionExecutor
{
    public AutomationStepType StepType => AutomationStepType.UpdateContactGroup;

    public Task<StepExecutionResult> ExecuteAsync(AutomationContext context)
    {
        var config = context.CurrentStep.GetConfiguration<UpdateContactGroupStepConfig>();
        if (config == null)
        {
            return Task.FromResult(StepExecutionResult.Failed("UpdateContactGroup step is missing configuration"));
        }

        try
        {
            var contactGroup = CMS.ContactManagement.ContactGroupInfo.Provider.Get()
                .WhereEquals("ContactGroupName", config.ContactGroupCodeName)
                .FirstOrDefault();

            if (contactGroup == null)
            {
                return Task.FromResult(StepExecutionResult.Failed(
                    $"Contact group '{config.ContactGroupCodeName}' not found"));
            }

            if (config.Add)
            {
                var membership = new CMS.ContactManagement.ContactGroupMemberInfo
                {
                    ContactGroupMemberContactGroupID = contactGroup.ContactGroupID,
                    ContactGroupMemberRelatedID = context.ContactId,
                    ContactGroupMemberType = CMS.ContactManagement.ContactGroupMemberTypeEnum.Contact,
                    ContactGroupMemberFromManual = true
                };
                CMS.ContactManagement.ContactGroupMemberInfo.Provider.Set(membership);

                logger.LogInformation(
                    "Added contact {ContactId} to group {GroupName}",
                    context.ContactId, config.ContactGroupCodeName);
            }
            else
            {
                var existing = CMS.ContactManagement.ContactGroupMemberInfo.Provider.Get()
                    .WhereEquals("ContactGroupMemberContactGroupID", contactGroup.ContactGroupID)
                    .WhereEquals("ContactGroupMemberRelatedID", context.ContactId)
                    .WhereEquals("ContactGroupMemberType", (int)CMS.ContactManagement.ContactGroupMemberTypeEnum.Contact)
                    .FirstOrDefault();

                if (existing != null)
                {
                    CMS.ContactManagement.ContactGroupMemberInfo.Provider.Delete(existing);
                }

                logger.LogInformation(
                    "Removed contact {ContactId} from group {GroupName}",
                    context.ContactId, config.ContactGroupCodeName);
            }

            return Task.FromResult(StepExecutionResult.Succeeded());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating contact group for contact {ContactId}", context.ContactId);
            return Task.FromResult(StepExecutionResult.Failed($"Contact group update failed: {ex.Message}"));
        }
    }
}

/// <summary>
/// Calls an external webhook/API endpoint.
/// Supports templated request bodies with contact and trigger data placeholders.
/// </summary>
public class CallWebhookActionExecutor(
    IHttpClientFactory httpClientFactory,
    ILogger<CallWebhookActionExecutor> logger) : IAutomationActionExecutor
{
    public AutomationStepType StepType => AutomationStepType.CallWebhook;

    public async Task<StepExecutionResult> ExecuteAsync(AutomationContext context)
    {
        var config = context.CurrentStep.GetConfiguration<CallWebhookStepConfig>();
        if (config == null)
        {
            return StepExecutionResult.Failed("CallWebhook step is missing configuration");
        }

        try
        {
            using var client = httpClientFactory.CreateClient("AutomationWebhook");
            client.Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds);

            var body = ReplacePlaceholders(config.BodyTemplate, context);

            var request = new HttpRequestMessage(
                new HttpMethod(config.Method),
                config.Url);

            if (!string.IsNullOrEmpty(body))
            {
                request.Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
            }

            if (!string.IsNullOrEmpty(config.Headers))
            {
                var headers = JsonSerializer.Deserialize<Dictionary<string, string>>(config.Headers);
                if (headers != null)
                {
                    foreach (var (key, value) in headers)
                    {
                        request.Headers.TryAddWithoutValidation(key, value);
                    }
                }
            }

            var response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                logger.LogWarning(
                    "Webhook call to {Url} returned {StatusCode}: {Response}",
                    config.Url, response.StatusCode, responseBody);
                return StepExecutionResult.Failed(
                    $"Webhook returned {response.StatusCode}: {responseBody}");
            }

            logger.LogInformation(
                "Webhook call to {Url} succeeded for contact {ContactId}",
                config.Url, context.ContactId);

            return StepExecutionResult.Succeeded();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error calling webhook for contact {ContactId}", context.ContactId);
            return StepExecutionResult.Failed($"Webhook call failed: {ex.Message}");
        }
    }

    private static string? ReplacePlaceholders(string? template, AutomationContext context)
    {
        if (string.IsNullOrEmpty(template))
        {
            return null;
        }

        var contact = CMS.ContactManagement.ContactInfo.Provider.Get(context.ContactId);
        if (contact == null)
        {
            return template;
        }

        return template
            .Replace("{ContactId}", context.ContactId.ToString(), StringComparison.OrdinalIgnoreCase)
            .Replace("{ContactEmail}", contact.ContactEmail ?? "", StringComparison.OrdinalIgnoreCase)
            .Replace("{ContactFirstName}", contact.ContactFirstName ?? "", StringComparison.OrdinalIgnoreCase)
            .Replace("{ContactLastName}", contact.ContactLastName ?? "", StringComparison.OrdinalIgnoreCase)
            .Replace("{ProcessName}", context.Process.Name, StringComparison.OrdinalIgnoreCase)
            .Replace("{StepName}", context.CurrentStep.Name, StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// Sends an internal notification email to configured recipients.
/// </summary>
public class SendNotificationActionExecutor(
    CMS.EmailEngine.IEmailService emailService,
    ILogger<SendNotificationActionExecutor> logger) : IAutomationActionExecutor
{
    public AutomationStepType StepType => AutomationStepType.SendNotification;

    public async Task<StepExecutionResult> ExecuteAsync(AutomationContext context)
    {
        var config = context.CurrentStep.GetConfiguration<SendNotificationStepConfig>();
        if (config == null)
        {
            return StepExecutionResult.Failed("SendNotification step is missing configuration");
        }

        try
        {
            if (string.IsNullOrWhiteSpace(config.RecipientEmails))
            {
                return StepExecutionResult.Failed("No recipient emails configured");
            }

            var contact = CMS.ContactManagement.ContactInfo.Provider.Get(context.ContactId);
            var contactName = contact != null
                ? $"{contact.ContactFirstName} {contact.ContactLastName}".Trim()
                : $"Contact {context.ContactId}";

            var subject = config.EmailSubject
                .Replace("{ContactName}", contactName, StringComparison.OrdinalIgnoreCase)
                .Replace("{ProcessName}", context.Process.Name, StringComparison.OrdinalIgnoreCase);

            var emailMessage = new CMS.EmailEngine.EmailMessage
            {
                Recipients = config.RecipientEmails,
                Subject = subject,
                From = "automation@baseline.local"
            };

            await emailService.SendEmail(emailMessage);

            logger.LogInformation(
                "Sent notification to {Recipients} for process {ProcessName}, contact {ContactId}",
                config.RecipientEmails, context.Process.Name, context.ContactId);

            return StepExecutionResult.Succeeded();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending notification for contact {ContactId}", context.ContactId);
            return StepExecutionResult.Failed($"Notification failed: {ex.Message}");
        }
    }
}
