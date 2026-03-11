using Microsoft.Extensions.Logging;

namespace Baseline.Automation.Services;

/// <summary>
/// Evaluates conditions for branching in automation processes.
/// Supports core contact-based conditions with extension points for custom logic.
/// </summary>
public class AutomationConditionEvaluator(
    ILogger<AutomationConditionEvaluator> logger) : IAutomationConditionEvaluator
{
    /// <inheritdoc/>
    public async Task<bool> EvaluateAsync(int contactId, ConditionStepConfig config, string? triggerData = null)
    {
        ArgumentNullException.ThrowIfNull(config);

        logger.LogDebug(
            "Evaluating condition {ConditionType} for contact {ContactId}",
            config.ConditionType, contactId);

        try
        {
            return config.ConditionType switch
            {
                ConditionType.ContactField => await EvaluateContactFieldAsync(contactId, config),
                ConditionType.ContactGroup => await EvaluateContactGroupAsync(contactId, config),
                ConditionType.ActivityPerformed => await EvaluateActivityPerformedAsync(contactId, config),
                ConditionType.IsSubscribed => await EvaluateIsSubscribedAsync(contactId, config),
                ConditionType.IsMember => await EvaluateIsMemberAsync(contactId, config),
                ConditionType.HasConsent => await EvaluateHasConsentAsync(contactId, config),
                ConditionType.CustomExpression => await EvaluateCustomExpressionAsync(contactId, config, triggerData),
                _ => throw new InvalidOperationException($"Unknown condition type: {config.ConditionType}")
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error evaluating condition {ConditionType} for contact {ContactId}",
                config.ConditionType, contactId);
            return false;
        }
    }

    /// <summary>
    /// Evaluates a contact field value against a comparison.
    /// </summary>
    protected virtual Task<bool> EvaluateContactFieldAsync(int contactId, ConditionStepConfig config)
    {
        var contact = CMS.ContactManagement.ContactInfo.Provider.Get(contactId);
        if (contact == null)
        {
            return Task.FromResult(false);
        }

        var fieldValue = contact.GetValue(config.FieldName)?.ToString() ?? string.Empty;
        return Task.FromResult(CompareValues(fieldValue, config.CompareValue ?? string.Empty, config.Operator));
    }

    /// <summary>
    /// Checks if the contact is in a specific contact group.
    /// </summary>
    protected virtual Task<bool> EvaluateContactGroupAsync(int contactId, ConditionStepConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.CompareValue))
        {
            return Task.FromResult(false);
        }

        var isMember = CMS.ContactManagement.ContactGroupMemberInfo.Provider.Get()
            .WhereEquals("ContactGroupMemberRelatedID", contactId)
            .WhereEquals("ContactGroupMemberType", 0) // Contact type
            .Any();

        return Task.FromResult(isMember);
    }

    /// <summary>
    /// Checks if the contact has performed a specific activity.
    /// </summary>
    protected virtual Task<bool> EvaluateActivityPerformedAsync(int contactId, ConditionStepConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.CompareValue))
        {
            return Task.FromResult(false);
        }

        var hasActivity = CMS.Activities.ActivityInfo.Provider.Get()
            .WhereEquals("ActivityContactID", contactId)
            .WhereEquals("ActivityType", config.CompareValue)
            .TopN(1)
            .Any();

        return Task.FromResult(hasActivity);
    }

    /// <summary>
    /// Checks if the contact is subscribed to a recipient list.
    /// Override for custom subscription checking.
    /// </summary>
    protected virtual Task<bool> EvaluateIsSubscribedAsync(int contactId, ConditionStepConfig config)
    {
        logger.LogDebug("IsSubscribed condition evaluated for contact {ContactId}", contactId);
        return Task.FromResult(false);
    }

    /// <summary>
    /// Checks if the contact has an active member account.
    /// </summary>
    protected virtual Task<bool> EvaluateIsMemberAsync(int contactId, ConditionStepConfig config)
    {
        var contact = CMS.ContactManagement.ContactInfo.Provider.Get(contactId);
        if (contact == null)
        {
            return Task.FromResult(false);
        }

        var memberId = contact.GetIntegerValue("ContactMemberID", 0);
        return Task.FromResult(memberId > 0);
    }

    /// <summary>
    /// Checks if the contact has given a specific consent.
    /// Override for site-specific implementation.
    /// </summary>
    protected virtual Task<bool> EvaluateHasConsentAsync(int contactId, ConditionStepConfig config)
    {
        logger.LogDebug("HasConsent condition — override for site-specific implementation");
        return Task.FromResult(false);
    }

    /// <summary>
    /// Evaluates a custom expression.
    /// Override for custom expression language support.
    /// </summary>
    protected virtual Task<bool> EvaluateCustomExpressionAsync(int contactId, ConditionStepConfig config, string? triggerData)
    {
        logger.LogWarning("CustomExpression conditions require a site-specific implementation");
        return Task.FromResult(false);
    }

    /// <summary>
    /// Compares two string values using the specified operator.
    /// </summary>
    protected static bool CompareValues(string actual, string expected, ComparisonOperator op) => op switch
    {
        ComparisonOperator.Equals => string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase),
        ComparisonOperator.NotEquals => !string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase),
        ComparisonOperator.Contains => actual.Contains(expected, StringComparison.OrdinalIgnoreCase),
        ComparisonOperator.NotContains => !actual.Contains(expected, StringComparison.OrdinalIgnoreCase),
        ComparisonOperator.StartsWith => actual.StartsWith(expected, StringComparison.OrdinalIgnoreCase),
        ComparisonOperator.EndsWith => actual.EndsWith(expected, StringComparison.OrdinalIgnoreCase),
        ComparisonOperator.IsEmpty => string.IsNullOrWhiteSpace(actual),
        ComparisonOperator.IsNotEmpty => !string.IsNullOrWhiteSpace(actual),
        ComparisonOperator.GreaterThan =>
            decimal.TryParse(actual, out var av1) && decimal.TryParse(expected, out var ev1) && av1 > ev1,
        ComparisonOperator.GreaterThanOrEqual =>
            decimal.TryParse(actual, out var av2) && decimal.TryParse(expected, out var ev2) && av2 >= ev2,
        ComparisonOperator.LessThan =>
            decimal.TryParse(actual, out var av3) && decimal.TryParse(expected, out var ev3) && av3 < ev3,
        ComparisonOperator.LessThanOrEqual =>
            decimal.TryParse(actual, out var av4) && decimal.TryParse(expected, out var ev4) && av4 <= ev4,
        _ => false
    };
}
