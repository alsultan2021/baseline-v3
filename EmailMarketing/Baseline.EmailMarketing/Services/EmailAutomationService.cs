using CMS.Notifications;
using Microsoft.Extensions.Logging;

namespace Baseline.EmailMarketing;

/// <summary>
/// Types of automated email triggers.
/// </summary>
public enum EmailAutomationTrigger
{
    /// <summary>Cart abandoned after configured delay.</summary>
    AbandonedCart,

    /// <summary>Follow-up email after purchase.</summary>
    PostPurchase,

    /// <summary>Re-engagement for inactive subscribers.</summary>
    ReEngagement,

    /// <summary>Welcome email for new subscribers.</summary>
    WelcomeSeries,

    /// <summary>Birthday / anniversary emails.</summary>
    Milestone
}

/// <summary>
/// Configuration for an automated email workflow.
/// </summary>
public record EmailAutomationConfig(
    EmailAutomationTrigger Trigger,
    string NotificationTemplateName,
    TimeSpan Delay,
    bool Enabled = true);

/// <summary>
/// Service for scheduling and executing automated email workflows
/// triggered by commerce and marketing events.
/// </summary>
public interface IEmailAutomationService
{
    /// <summary>
    /// Enqueue an automation trigger for a contact email.
    /// The email will be sent after the configured delay if the trigger is still valid.
    /// </summary>
    Task EnqueueAsync(EmailAutomationTrigger trigger, string contactEmail, Dictionary<string, string>? data = null, CancellationToken ct = default);

    /// <summary>
    /// Cancel a pending automation trigger (e.g. cart recovered before delay expires).
    /// </summary>
    Task CancelAsync(EmailAutomationTrigger trigger, string contactEmail, CancellationToken ct = default);

    /// <summary>
    /// Process all due automation triggers. Called by a background worker.
    /// </summary>
    Task ProcessDueTriggersAsync(CancellationToken ct = default);
}

/// <summary>
/// In-memory implementation of <see cref="IEmailAutomationService"/>.
/// Production deployments should swap this for a durable queue (e.g. database or Azure Queue).
/// </summary>
public class EmailAutomationService(
    INotificationEmailMessageProvider notificationEmailMessageProvider,
    ILogger<EmailAutomationService> logger) : IEmailAutomationService
{
    private static readonly Dictionary<string, (EmailAutomationTrigger Trigger, DateTimeOffset DueAt, Dictionary<string, string> Data)> _queue = new();
    private static readonly object _lock = new();

    private static readonly Dictionary<EmailAutomationTrigger, EmailAutomationConfig> _configs = new()
    {
        [EmailAutomationTrigger.AbandonedCart] = new(EmailAutomationTrigger.AbandonedCart, "AbandonedCartReminder", TimeSpan.FromHours(1)),
        [EmailAutomationTrigger.PostPurchase] = new(EmailAutomationTrigger.PostPurchase, "PostPurchaseFollowUp", TimeSpan.FromDays(3)),
        [EmailAutomationTrigger.ReEngagement] = new(EmailAutomationTrigger.ReEngagement, "ReEngagementCampaign", TimeSpan.FromDays(30)),
        [EmailAutomationTrigger.WelcomeSeries] = new(EmailAutomationTrigger.WelcomeSeries, "WelcomeEmail", TimeSpan.FromMinutes(5)),
        [EmailAutomationTrigger.Milestone] = new(EmailAutomationTrigger.Milestone, "MilestoneEmail", TimeSpan.Zero),
    };

    /// <inheritdoc/>
    public Task EnqueueAsync(EmailAutomationTrigger trigger, string contactEmail, Dictionary<string, string>? data = null, CancellationToken ct = default)
    {
        if (!_configs.TryGetValue(trigger, out var config) || !config.Enabled)
        {
            return Task.CompletedTask;
        }

        string key = $"{trigger}:{contactEmail}";
        lock (_lock)
        {
            _queue[key] = (trigger, DateTimeOffset.UtcNow.Add(config.Delay), data ?? []);
        }

        logger.LogDebug("Enqueued {Trigger} for {Email}, due at {DueAt}", trigger, contactEmail, DateTimeOffset.UtcNow.Add(config.Delay));
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task CancelAsync(EmailAutomationTrigger trigger, string contactEmail, CancellationToken ct = default)
    {
        string key = $"{trigger}:{contactEmail}";
        lock (_lock)
        {
            _queue.Remove(key);
        }

        logger.LogDebug("Cancelled {Trigger} automation for {Email}", trigger, contactEmail);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task ProcessDueTriggersAsync(CancellationToken ct = default)
    {
        List<(string Key, string Email, EmailAutomationTrigger Trigger, Dictionary<string, string> Data)> due;

        lock (_lock)
        {
            var now = DateTimeOffset.UtcNow;
            due = _queue
                .Where(kv => kv.Value.DueAt <= now)
                .Select(kv =>
                {
                    string email = kv.Key[(kv.Key.IndexOf(':') + 1)..];
                    return (kv.Key, email, kv.Value.Trigger, kv.Value.Data);
                })
                .ToList();

            foreach (var item in due)
            {
                _queue.Remove(item.Key);
            }
        }

        foreach (var item in due)
        {
            if (ct.IsCancellationRequested) break;

            if (!_configs.TryGetValue(item.Trigger, out var config))
                continue;

            try
            {
                var placeholders = new AutomationEmailPlaceholders(config.NotificationTemplateName, item.Data);
                await notificationEmailMessageProvider.CreateEmailMessage(
                    placeholders.NotificationEmailName, 0, placeholders);
                logger.LogInformation("Sent {Trigger} automation email to {Email}", item.Trigger, item.Email);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send {Trigger} automation email to {Email}", item.Trigger, item.Email);
            }
        }
    }
}

/// <summary>
/// Generic placeholder bridge for automation notification templates.
/// Wraps a <see cref="Dictionary{String,String}"/> as <see cref="INotificationEmailPlaceholdersByCodeName"/>.
/// </summary>
public class AutomationEmailPlaceholders(string templateName, Dictionary<string, string> data)
    : INotificationEmailPlaceholdersByCodeName
{
    public string NotificationEmailName => templateName;

    public Dictionary<string, Func<string>> GetPlaceholders()
        => data.ToDictionary(kv => kv.Key, kv => (Func<string>)(() => kv.Value));
}
