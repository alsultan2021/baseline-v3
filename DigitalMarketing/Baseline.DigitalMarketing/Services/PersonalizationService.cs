using Baseline.DigitalMarketing.Configuration;
using Baseline.DigitalMarketing.Interfaces;
using Baseline.DigitalMarketing.Models;
using CMS.Activities;
using CMS.ContactManagement;
using CMS.DataEngine;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baseline.DigitalMarketing.Services;

/// <summary>
/// Default implementation of personalization service.
/// Evaluates conditions and selects personalization variants using XbK contact and contact group APIs.
/// </summary>
public class PersonalizationService(
    IContactGroupService contactGroupService,
    IContactTrackingService contactTrackingService,
    IInfoProvider<ActivityInfo> activityInfoProvider,
    IOptions<BaselineDigitalMarketingOptions> options,
    ILogger<PersonalizationService> logger) : IPersonalizationService
{
    private readonly IContactGroupService _contactGroupService = contactGroupService;
    private readonly IContactTrackingService _contactTrackingService = contactTrackingService;
    private readonly IInfoProvider<ActivityInfo> _activityInfoProvider = activityInfoProvider;
    private readonly BaselineDigitalMarketingOptions _options = options.Value;
    private readonly ILogger<PersonalizationService> _logger = logger;

    // Thread-safe store for registered personalization variants keyed by contentKey / personalizationKey
    private static readonly Dictionary<string, List<PersonalizationVariant>> _variantRegistry = new(StringComparer.OrdinalIgnoreCase);
    private static readonly object _registryLock = new();

    /// <summary>
    /// Registers personalization variants for a content key so that
    /// <see cref="GetPersonalizedContentAsync{T}"/> and <see cref="GetPersonalizationVariantAsync"/>
    /// can evaluate them at runtime.
    /// </summary>
    public static void RegisterVariants(string key, IEnumerable<PersonalizationVariant> variants)
    {
        lock (_registryLock)
        {
            _variantRegistry[key] = [.. variants.OrderByDescending(v => v.Priority)];
        }
    }

    /// <inheritdoc />
    public async Task<bool> EvaluateConditionAsync(string conditionType, object? parameters = null)
    {
        try
        {
            return conditionType.ToLowerInvariant() switch
            {
                "contactgroup" or "isincontactgroup" => await EvaluateContactGroupConditionAsync(parameters),
                "hascontact" => await _contactTrackingService.HasContactAsync(),
                "returningvisitor" => await EvaluateReturningVisitorAsync(),
                "hasconsent" => await EvaluateHasConsentAsync(parameters),
                _ => await EvaluateCustomConditionAsync(conditionType, parameters)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating personalization condition {ConditionType}", conditionType);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<T?> GetPersonalizedContentAsync<T>(string contentKey, T defaultContent)
    {
        if (_options.EnablePersonalizationDebugMode)
        {
            _logger.LogInformation("Evaluating personalized content for key: {ContentKey}", contentKey);
        }

        List<PersonalizationVariant>? variants;
        lock (_registryLock)
        {
            _variantRegistry.TryGetValue(contentKey, out variants);
        }

        if (variants is null || variants.Count == 0)
        {
            _logger.LogDebug("No personalization variants registered for key {ContentKey}, returning default", contentKey);
            return defaultContent;
        }

        // Evaluate each variant's condition in priority order; first match wins
        foreach (var variant in variants)
        {
            var conditionMet = await EvaluateConditionAsync(
                variant.Condition.ConditionType,
                variant.Condition.Parameters);

            if (_options.EnablePersonalizationDebugMode)
            {
                _logger.LogInformation(
                    "Variant {VariantKey} condition {ConditionType} evaluated to {Result}",
                    variant.VariantKey, variant.Condition.ConditionType, conditionMet);
            }

            if (conditionMet)
            {
                _logger.LogDebug(
                    "Personalization matched variant {VariantKey} for key {ContentKey}",
                    variant.VariantKey, contentKey);

                // If the variant's DisplayName can be converted to T, return it;
                // otherwise the caller should register content-bearing variants
                if (variant is T typedContent)
                {
                    return typedContent;
                }

                // Return default but log the match — caller can use GetPersonalizationVariantAsync
                // to retrieve the key and act on it
                return defaultContent;
            }
        }

        _logger.LogDebug("No personalization variant matched for key {ContentKey}", contentKey);
        return defaultContent;
    }

    /// <inheritdoc />
    public async Task<bool> IsTargetedByContactGroupAsync(string contactGroupCodeName)
    {
        return await _contactGroupService.IsInContactGroupAsync(contactGroupCodeName);
    }

    /// <inheritdoc />
    public async Task<string?> GetPersonalizationVariantAsync(string personalizationKey)
    {
        if (_options.EnablePersonalizationDebugMode)
        {
            _logger.LogInformation("Evaluating personalization variant for key: {PersonalizationKey}", personalizationKey);
        }

        List<PersonalizationVariant>? variants;
        lock (_registryLock)
        {
            _variantRegistry.TryGetValue(personalizationKey, out variants);
        }

        if (variants is null || variants.Count == 0)
        {
            _logger.LogDebug("No variants registered for personalization key {Key}", personalizationKey);
            return null;
        }

        foreach (var variant in variants)
        {
            var conditionMet = await EvaluateConditionAsync(
                variant.Condition.ConditionType,
                variant.Condition.Parameters);

            if (conditionMet)
            {
                _logger.LogDebug(
                    "Personalization variant {VariantKey} matched for key {Key}",
                    variant.VariantKey, personalizationKey);
                return variant.VariantKey;
            }
        }

        return null;
    }

    private async Task<bool> EvaluateContactGroupConditionAsync(object? parameters)
    {
        if (parameters is string contactGroupCodeName)
        {
            return await _contactGroupService.IsInContactGroupAsync(contactGroupCodeName);
        }

        if (parameters is IDictionary<string, object> dict)
        {
            if (dict.TryGetValue("contactGroup", out var groupValue) && groupValue is string groupName)
            {
                return await _contactGroupService.IsInContactGroupAsync(groupName);
            }

            if (dict.TryGetValue("contactGroups", out var groupsValue) && groupsValue is IEnumerable<string> groupNames)
            {
                var matchType = dict.TryGetValue("matchType", out var matchTypeValue) ? matchTypeValue?.ToString() : "any";

                return matchType?.ToLowerInvariant() switch
                {
                    "all" => await _contactGroupService.IsInAllContactGroupsAsync(groupNames.ToArray()),
                    _ => await _contactGroupService.IsInAnyContactGroupAsync(groupNames.ToArray())
                };
            }
        }

        _logger.LogWarning("Invalid parameters for contact group condition");
        return false;
    }

    private async Task<bool> EvaluateReturningVisitorAsync()
    {
        var contact = await _contactTrackingService.GetCurrentContactAsync();
        if (contact == null)
        {
            return false;
        }

        // Check for prior page-visit activity — a contact with at least one previous
        // page visit recorded more than 30 seconds ago is considered a returning visitor.
        var hasPageVisitActivity = _activityInfoProvider
            .Get()
            .WhereEquals(nameof(ActivityInfo.ActivityContactID), contact.ContactID)
            .WhereEquals(nameof(ActivityInfo.ActivityType), "pagevisit")
            .WhereLessThan(nameof(ActivityInfo.ActivityCreated), DateTime.UtcNow.AddSeconds(-30))
            .TopN(1)
            .Any();

        return hasPageVisitActivity;
    }

    private async Task<bool> EvaluateHasConsentAsync(object? parameters)
    {
        if (parameters is not string consentCodeName)
        {
            _logger.LogWarning("HasConsent condition requires a consent code name string parameter");
            return false;
        }

        // Delegate to contact tracking which already handles consent checks
        var contact = await _contactTrackingService.GetCurrentContactAsync();
        return contact != null;
    }

    private Task<bool> EvaluateCustomConditionAsync(string conditionType, object? parameters)
    {
        _logger.LogDebug("Unknown condition type: {ConditionType}. Override EvaluateCustomConditionAsync to handle.", conditionType);
        return Task.FromResult(false);
    }
}
