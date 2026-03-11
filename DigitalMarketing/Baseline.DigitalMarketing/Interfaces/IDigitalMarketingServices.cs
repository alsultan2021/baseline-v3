using CMS.ContactManagement;

namespace Baseline.DigitalMarketing.Interfaces;

/// <summary>
/// Service for tracking and managing website visitor contacts.
/// </summary>
public interface IContactTrackingService
{
    /// <summary>
    /// Gets the current contact for the website visitor, if available.
    /// </summary>
    Task<ContactInfo?> GetCurrentContactAsync();

    /// <summary>
    /// Gets or creates a contact for the current website visitor.
    /// </summary>
    Task<ContactInfo> EnsureContactAsync();

    /// <summary>
    /// Updates the specified contact with new information.
    /// </summary>
    Task UpdateContactAsync(ContactInfo contact);

    /// <summary>
    /// Checks if the current visitor has an associated contact.
    /// </summary>
    Task<bool> HasContactAsync();

    /// <summary>
    /// Gets the current contact's email address, if available.
    /// </summary>
    Task<string?> GetContactEmailAsync();
}

/// <summary>
/// Service for logging visitor activities.
/// </summary>
public interface IActivityLoggingService
{
    /// <summary>
    /// Logs a standard activity for the current contact.
    /// </summary>
    /// <param name="activityType">The activity type code name.</param>
    /// <param name="title">The activity title.</param>
    /// <param name="value">Optional activity value.</param>
    Task LogActivityAsync(string activityType, string title, string? value = null);

    /// <summary>
    /// Logs a page visit activity.
    /// </summary>
    /// <param name="pageUrl">The URL of the visited page.</param>
    /// <param name="pageTitle">The title of the visited page.</param>
    Task LogPageVisitAsync(string pageUrl, string pageTitle);

    /// <summary>
    /// Logs a custom activity with additional data.
    /// </summary>
    /// <param name="activityType">The custom activity type code name.</param>
    /// <param name="data">Optional dictionary of additional data.</param>
    Task LogCustomActivityAsync(string activityType, IDictionary<string, string>? data = null);

    /// <summary>
    /// Logs a form submission activity.
    /// </summary>
    /// <param name="formCodeName">The form code name.</param>
    /// <param name="formDisplayName">The form display name.</param>
    Task LogFormSubmissionAsync(string formCodeName, string formDisplayName);

    /// <summary>
    /// Logs a newsletter subscription activity.
    /// </summary>
    /// <param name="recipientListCodeName">The recipient list code name.</param>
    Task LogNewsletterSubscriptionAsync(string recipientListCodeName);
}

/// <summary>
/// Service for evaluating contact group membership.
/// </summary>
public interface IContactGroupService
{
    /// <summary>
    /// Checks if the current contact is in the specified contact group.
    /// </summary>
    /// <param name="contactGroupCodeName">The contact group code name.</param>
    Task<bool> IsInContactGroupAsync(string contactGroupCodeName);

    /// <summary>
    /// Checks if the current contact is in any of the specified contact groups.
    /// </summary>
    /// <param name="contactGroupCodeNames">Array of contact group code names.</param>
    Task<bool> IsInAnyContactGroupAsync(params string[] contactGroupCodeNames);

    /// <summary>
    /// Checks if the current contact is in all of the specified contact groups.
    /// </summary>
    /// <param name="contactGroupCodeNames">Array of contact group code names.</param>
    Task<bool> IsInAllContactGroupsAsync(params string[] contactGroupCodeNames);

    /// <summary>
    /// Gets all contact groups that the current contact belongs to.
    /// </summary>
    Task<IEnumerable<string>> GetContactGroupsAsync();

    /// <summary>
    /// Gets the count of contacts in the specified group.
    /// </summary>
    /// <param name="contactGroupCodeName">The contact group code name.</param>
    Task<int> GetContactGroupMemberCountAsync(string contactGroupCodeName);
}

/// <summary>
/// Service for content personalization based on contact data.
/// </summary>
public interface IPersonalizationService
{
    /// <summary>
    /// Evaluates a personalization condition for the current contact.
    /// </summary>
    /// <param name="conditionType">The condition type identifier.</param>
    /// <param name="parameters">Optional condition parameters.</param>
    Task<bool> EvaluateConditionAsync(string conditionType, object? parameters = null);

    /// <summary>
    /// Gets personalized content based on the current contact's profile.
    /// </summary>
    /// <typeparam name="T">The content type.</typeparam>
    /// <param name="contentKey">A key identifying the personalized content.</param>
    /// <param name="defaultContent">The default content if no personalization applies.</param>
    Task<T?> GetPersonalizedContentAsync<T>(string contentKey, T defaultContent);

    /// <summary>
    /// Evaluates if the current contact matches a contact group-based condition.
    /// </summary>
    /// <param name="contactGroupCodeName">The contact group code name.</param>
    Task<bool> IsTargetedByContactGroupAsync(string contactGroupCodeName);

    /// <summary>
    /// Gets the personalization variant key for the current contact.
    /// </summary>
    /// <param name="personalizationKey">The personalization configuration key.</param>
    Task<string?> GetPersonalizationVariantAsync(string personalizationKey);
}

/// <summary>
/// Service for managing custom activity types.
/// </summary>
public interface ICustomActivityTypeService
{
    /// <summary>
    /// Registers a custom activity type in the system.
    /// </summary>
    /// <param name="codeName">The activity type code name.</param>
    /// <param name="displayName">The activity type display name.</param>
    /// <param name="description">Optional description.</param>
    Task RegisterCustomActivityTypeAsync(string codeName, string displayName, string? description = null);

    /// <summary>
    /// Gets all registered custom activity types.
    /// </summary>
    Task<IEnumerable<CustomActivityTypeInfo>> GetCustomActivityTypesAsync();

    /// <summary>
    /// Checks if an activity type exists.
    /// </summary>
    /// <param name="codeName">The activity type code name.</param>
    Task<bool> ActivityTypeExistsAsync(string codeName);
}

/// <summary>
/// Information about a custom activity type.
/// </summary>
public record CustomActivityTypeInfo(
    string CodeName,
    string DisplayName,
    string? Description,
    bool IsCustom
);
