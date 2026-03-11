using CMS.Activities;
using CMS.EmailEngine;
using Microsoft.Extensions.Logging;

namespace Baseline.Automation.Actions;

/// <summary>
/// Executes the Send Email step.
/// </summary>
public class SendEmailActionExecutor(
    IEmailService emailService,
    ILogger<SendEmailActionExecutor> logger) : IAutomationActionExecutor
{
    public AutomationStepType StepType => AutomationStepType.SendEmail;

    public async Task<StepExecutionResult> ExecuteAsync(AutomationContext context)
    {
        var config = context.CurrentStep.GetConfiguration<SendEmailStepConfig>();
        if (config == null)
        {
            return StepExecutionResult.Failed("SendEmail step is missing configuration");
        }

        try
        {
            var contact = CMS.ContactManagement.ContactInfo.Provider.Get(context.ContactId);
            if (contact == null)
            {
                return StepExecutionResult.Failed($"Contact {context.ContactId} not found");
            }

            var email = contact.ContactEmail;
            if (string.IsNullOrWhiteSpace(email))
            {
                logger.LogWarning("Contact {ContactId} has no email address, skipping SendEmail step", context.ContactId);
                return StepExecutionResult.Failed("Contact has no email address");
            }

            var emailMessage = new EmailMessage
            {
                Recipients = email,
                Subject = $"Automation: {context.Process.Name}",
                From = "automation@baseline.local"
            };

            await emailService.SendEmail(emailMessage);

            logger.LogInformation(
                "Sent email to {Email} for process {ProcessName}, step {StepName}",
                email, context.Process.Name, context.CurrentStep.Name);

            return StepExecutionResult.Succeeded();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending email for contact {ContactId}", context.ContactId);
            return StepExecutionResult.Failed($"Email sending failed: {ex.Message}");
        }
    }
}

/// <summary>
/// Executes the Wait step.
/// </summary>
public class WaitActionExecutor(
    ILogger<WaitActionExecutor> logger) : IAutomationActionExecutor
{
    public AutomationStepType StepType => AutomationStepType.Wait;

    public Task<StepExecutionResult> ExecuteAsync(AutomationContext context)
    {
        var config = context.CurrentStep.GetConfiguration<WaitStepConfig>();
        if (config == null)
        {
            return Task.FromResult(StepExecutionResult.Failed("Wait step is missing configuration"));
        }

        DateTimeOffset waitUntil;
        if (config.UntilDate.HasValue)
        {
            waitUntil = config.UntilDate.Value;
            if (waitUntil <= DateTimeOffset.UtcNow)
            {
                logger.LogDebug("Wait date {WaitUntil} is in the past, advancing immediately", waitUntil);
                return Task.FromResult(StepExecutionResult.Succeeded());
            }
        }
        else if (config.IntervalMinutes.HasValue)
        {
            waitUntil = DateTimeOffset.UtcNow.AddMinutes(config.IntervalMinutes.Value);
        }
        else
        {
            return Task.FromResult(StepExecutionResult.Failed("Wait step must specify IntervalMinutes or UntilDate"));
        }

        logger.LogDebug(
            "Contact {ContactId} waiting until {WaitUntil} in process {ProcessName}",
            context.ContactId, waitUntil, context.Process.Name);

        return Task.FromResult(StepExecutionResult.WaitRequired(waitUntil));
    }
}

/// <summary>
/// Executes the Log Custom Activity step.
/// </summary>
public class LogActivityActionExecutor(
    IActivityLogService activityLogService,
    ILogger<LogActivityActionExecutor> logger) : IAutomationActionExecutor
{
    public AutomationStepType StepType => AutomationStepType.LogCustomActivity;

    public Task<StepExecutionResult> ExecuteAsync(AutomationContext context)
    {
        var config = context.CurrentStep.GetConfiguration<LogActivityStepConfig>();
        if (config == null)
        {
            return Task.FromResult(StepExecutionResult.Failed("LogCustomActivity step is missing configuration"));
        }

        try
        {
            var activityInitializer = new AutomationActivityInitializer(
                config.ActivityTypeName,
                config.Title ?? $"Automation: {context.Process.Name}")
            {
                ContactID = context.ContactId,
                Value = config.Value
            };

            activityLogService.Log(activityInitializer);

            logger.LogInformation(
                "Logged custom activity {ActivityType} for contact {ContactId}",
                config.ActivityTypeName, context.ContactId);

            return Task.FromResult(StepExecutionResult.Succeeded());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error logging custom activity for contact {ContactId}", context.ContactId);
            return Task.FromResult(StepExecutionResult.Failed($"Activity logging failed: {ex.Message}"));
        }
    }
}

/// <summary>
/// Executes the Set Contact Field Value step.
/// </summary>
public class SetContactFieldValueActionExecutor(
    ILogger<SetContactFieldValueActionExecutor> logger) : IAutomationActionExecutor
{
    public AutomationStepType StepType => AutomationStepType.SetContactFieldValue;

    public Task<StepExecutionResult> ExecuteAsync(AutomationContext context)
    {
        var config = context.CurrentStep.GetConfiguration<SetContactFieldStepConfig>();
        if (config == null)
        {
            return Task.FromResult(StepExecutionResult.Failed("SetContactFieldValue step is missing configuration"));
        }

        try
        {
            var contact = CMS.ContactManagement.ContactInfo.Provider.Get(context.ContactId);
            if (contact == null)
            {
                return Task.FromResult(StepExecutionResult.Failed($"Contact {context.ContactId} not found"));
            }

            contact.SetValue(config.FieldName, config.Value);
            CMS.ContactManagement.ContactInfo.Provider.Set(contact);

            logger.LogInformation(
                "Set contact field {FieldName} = {Value} for contact {ContactId}",
                config.FieldName, config.Value, context.ContactId);

            return Task.FromResult(StepExecutionResult.Succeeded());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error setting contact field for contact {ContactId}", context.ContactId);
            return Task.FromResult(StepExecutionResult.Failed($"Set contact field failed: {ex.Message}"));
        }
    }
}

/// <summary>
/// Executes the Condition step.
/// </summary>
public class ConditionActionExecutor(
    IAutomationConditionEvaluator conditionEvaluator,
    ILogger<ConditionActionExecutor> logger) : IAutomationActionExecutor
{
    public AutomationStepType StepType => AutomationStepType.Condition;

    public async Task<StepExecutionResult> ExecuteAsync(AutomationContext context)
    {
        var config = context.CurrentStep.GetConfiguration<ConditionStepConfig>();
        if (config == null)
        {
            return StepExecutionResult.Failed("Condition step is missing configuration");
        }

        try
        {
            var result = await conditionEvaluator.EvaluateAsync(
                context.ContactId, config, context.TriggerData);

            logger.LogDebug(
                "Condition {ConditionType} evaluated to {Result} for contact {ContactId}",
                config.ConditionType, result, context.ContactId);

            return StepExecutionResult.ConditionEvaluated(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error evaluating condition for contact {ContactId}", context.ContactId);
            return StepExecutionResult.Failed($"Condition evaluation failed: {ex.Message}");
        }
    }
}

/// <summary>
/// Activity initializer for the automation engine's LogActivity step.
/// </summary>
internal class AutomationActivityInitializer : IActivityInitializer
{
    public AutomationActivityInitializer(string activityType, string title)
    {
        ActivityType = activityType;
        Title = title;
    }

    public string ActivityType { get; }
    public string? Title { get; set; }
    public string? Value { get; set; }
    public int ContactID { get; set; }

    public string SettingsKeyName => string.Empty;

    public void Initialize(IActivityInfo activity)
    {
        activity.ActivityType = ActivityType;
        activity.ActivityTitle = Title ?? string.Empty;
        activity.ActivityValue = Value ?? string.Empty;
        activity.ActivityContactID = ContactID;
    }
}
