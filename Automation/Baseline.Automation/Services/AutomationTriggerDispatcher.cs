using System.Text.Json;
using Baseline.Automation.Configuration;
using CMS.ContactManagement;
using CMS.Membership;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baseline.Automation.Services;

/// <summary>
/// Dispatches automation trigger events to the automation engine.
/// Resolves contact IDs from member IDs and guards on <see cref="AutomationOptions.EnableAutomation"/>.
/// </summary>
public interface IAutomationTriggerDispatcher
{
    /// <summary>Fires a trigger event if automation is enabled.</summary>
    Task FireAsync(AutomationTriggerType triggerType, int contactId, object? data = null);

    /// <summary>Fires a trigger event, resolving the contact ID from a member ID.</summary>
    Task FireForMemberAsync(AutomationTriggerType triggerType, int memberId, object? data = null);

    /// <summary>Fires a trigger event, resolving the contact ID from an email address.</summary>
    Task FireForEmailAsync(AutomationTriggerType triggerType, string email, object? data = null);
}

/// <summary>
/// Default implementation. No-ops when automation is disabled via configuration.
/// </summary>
internal sealed class AutomationTriggerDispatcher(
    IAutomationEngine automationEngine,
    IOptions<AutomationOptions> options,
    ILogger<AutomationTriggerDispatcher> logger) : IAutomationTriggerDispatcher
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    /// <inheritdoc/>
    public async Task FireAsync(AutomationTriggerType triggerType, int contactId, object? data = null)
    {
        if (!options.Value.EnableAutomation)
        {
            return;
        }

        if (contactId <= 0)
        {
            logger.LogDebug("Skipping automation trigger {TriggerType}: invalid contact ID {ContactId}", triggerType, contactId);
            return;
        }

        try
        {
            var eventData = new TriggerEventData
            {
                TriggerType = triggerType,
                ContactId = contactId,
                Data = data is not null ? JsonSerializer.Serialize(data, JsonOptions) : null
            };

            var started = await automationEngine.FireTriggerAsync(eventData);

            if (started > 0)
            {
                logger.LogInformation(
                    "Automation trigger {TriggerType} fired for contact {ContactId}, started {Count} process(es)",
                    triggerType, contactId, started);
            }
        }
        catch (Exception ex)
        {
            // Never let automation failures break the calling operation
            logger.LogError(ex, "Failed to fire automation trigger {TriggerType} for contact {ContactId}", triggerType, contactId);
        }
    }

    /// <inheritdoc/>
    public async Task FireForMemberAsync(AutomationTriggerType triggerType, int memberId, object? data = null)
    {
        if (!options.Value.EnableAutomation)
        {
            return;
        }

        var contactId = ResolveContactFromMember(memberId);
        if (contactId.HasValue)
        {
            await FireAsync(triggerType, contactId.Value, data);
        }
        else
        {
            logger.LogDebug("No contact found for member {MemberId}, skipping trigger {TriggerType}", memberId, triggerType);
        }
    }

    /// <inheritdoc/>
    public async Task FireForEmailAsync(AutomationTriggerType triggerType, string email, object? data = null)
    {
        if (!options.Value.EnableAutomation || string.IsNullOrWhiteSpace(email))
        {
            return;
        }

        var contactId = ResolveContactFromEmail(email);
        if (contactId.HasValue)
        {
            await FireAsync(triggerType, contactId.Value, data);
        }
        else
        {
            logger.LogDebug("No contact found for email {Email}, skipping trigger {TriggerType}", email, triggerType);
        }
    }

    private static int? ResolveContactFromMember(int memberId)
    {
        var member = MemberInfo.Provider.Get(memberId);
        if (member is null || string.IsNullOrWhiteSpace(member.MemberEmail))
        {
            return null;
        }

        return ResolveContactFromEmail(member.MemberEmail);
    }

    private static int? ResolveContactFromEmail(string email)
    {
        var contact = ContactInfo.Provider.Get()
            .WhereEquals(nameof(ContactInfo.ContactEmail), email)
            .TopN(1)
            .FirstOrDefault();

        return contact?.ContactID;
    }
}

/// <summary>
/// No-op dispatcher used when automation is not registered.
/// </summary>
internal sealed class NullAutomationTriggerDispatcher : IAutomationTriggerDispatcher
{
    public Task FireAsync(AutomationTriggerType triggerType, int contactId, object? data = null) => Task.CompletedTask;
    public Task FireForMemberAsync(AutomationTriggerType triggerType, int memberId, object? data = null) => Task.CompletedTask;
    public Task FireForEmailAsync(AutomationTriggerType triggerType, string email, object? data = null) => Task.CompletedTask;
}
