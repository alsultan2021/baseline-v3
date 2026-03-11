using CMS.DataProtection;

namespace Baseline.DataProtection.Interfaces;

/// <summary>
/// Service for managing user consents.
/// </summary>
public interface IConsentService
{
    /// <summary>
    /// Gets all available consents defined in the system.
    /// </summary>
    Task<IEnumerable<ConsentInfo>> GetAvailableConsentsAsync();

    /// <summary>
    /// Gets a specific consent by code name.
    /// </summary>
    /// <param name="consentCodeName">The consent code name.</param>
    Task<ConsentInfo?> GetConsentAsync(string consentCodeName);

    /// <summary>
    /// Checks if the current contact has agreed to the specified consent.
    /// </summary>
    /// <param name="consentCodeName">The consent code name.</param>
    Task<bool> HasConsentAsync(string consentCodeName);

    /// <summary>
    /// Records agreement to the specified consent for the current contact.
    /// </summary>
    /// <param name="consentCodeName">The consent code name.</param>
    Task AgreeToConsentAsync(string consentCodeName);

    /// <summary>
    /// Revokes agreement to the specified consent for the current contact.
    /// </summary>
    /// <param name="consentCodeName">The consent code name.</param>
    Task RevokeConsentAsync(string consentCodeName);

    /// <summary>
    /// Gets all consents that the current contact has agreed to.
    /// </summary>
    Task<IEnumerable<string>> GetAgreedConsentsAsync();

    /// <summary>
    /// Gets the consent text for display.
    /// </summary>
    /// <param name="consentCodeName">The consent code name.</param>
    /// <param name="languageCode">Optional language code.</param>
    Task<string?> GetConsentTextAsync(string consentCodeName, string? languageCode = null);
}

/// <summary>
/// Service for handling data subject rights (GDPR).
/// </summary>
public interface IDataSubjectRightsService
{
    /// <summary>
    /// Exports personal data for the specified email address.
    /// </summary>
    /// <param name="email">The email address.</param>
    Task<byte[]> ExportPersonalDataAsync(string email);

    /// <summary>
    /// Requests erasure of personal data for the specified email address.
    /// </summary>
    /// <param name="email">The email address.</param>
    Task<DataErasureResult> RequestDataErasureAsync(string email);

    /// <summary>
    /// Gets the status of a data subject rights request.
    /// </summary>
    /// <param name="requestId">The request identifier.</param>
    Task<DataSubjectRightsStatus> GetRequestStatusAsync(string requestId);

    /// <summary>
    /// Gets all personal data associated with the specified email.
    /// </summary>
    /// <param name="email">The email address.</param>
    Task<PersonalDataSummary> GetPersonalDataSummaryAsync(string email);
}

/// <summary>
/// Service for managing cookie consent.
/// </summary>
public interface ICookieConsentService
{
    /// <summary>
    /// Gets the current cookie consent level for the visitor.
    /// </summary>
    Task<CookieConsentLevel> GetConsentLevelAsync();

    /// <summary>
    /// Sets the cookie consent level for the visitor.
    /// </summary>
    /// <param name="level">The consent level.</param>
    Task SetConsentLevelAsync(CookieConsentLevel level);

    /// <summary>
    /// Checks if cookie consent is required (based on regulations).
    /// </summary>
    bool IsConsentRequired();

    /// <summary>
    /// Checks if a specific cookie category is allowed.
    /// </summary>
    /// <param name="category">The cookie category.</param>
    Task<bool> IsCategoryAllowedAsync(CookieCategory category);

    /// <summary>
    /// Gets the current consent preferences.
    /// </summary>
    Task<CookieConsentPreferences> GetPreferencesAsync();

    /// <summary>
    /// Sets the consent preferences.
    /// </summary>
    /// <param name="preferences">The preferences to set.</param>
    Task SetPreferencesAsync(CookieConsentPreferences preferences);
}

/// <summary>
/// Cookie consent levels.
/// </summary>
public enum CookieConsentLevel
{
    /// <summary>
    /// No consent given.
    /// </summary>
    None = 0,

    /// <summary>
    /// Only strictly necessary cookies.
    /// </summary>
    Necessary = 1,

    /// <summary>
    /// Necessary and functional cookies.
    /// </summary>
    Functional = 2,

    /// <summary>
    /// Necessary, functional, and analytics cookies.
    /// </summary>
    Analytics = 3,

    /// <summary>
    /// All cookies including marketing/advertising.
    /// </summary>
    All = 4
}

/// <summary>
/// Cookie categories for granular consent.
/// </summary>
public enum CookieCategory
{
    /// <summary>
    /// Strictly necessary cookies (required for site to function).
    /// </summary>
    Necessary,

    /// <summary>
    /// Functional cookies (preferences, language).
    /// </summary>
    Functional,

    /// <summary>
    /// Analytics cookies (usage tracking).
    /// </summary>
    Analytics,

    /// <summary>
    /// Marketing/advertising cookies.
    /// </summary>
    Marketing
}

/// <summary>
/// Result of data erasure request.
/// </summary>
public record DataErasureResult(
    bool Success,
    string RequestId,
    string? Message,
    DateTime? EstimatedCompletionDate
);

/// <summary>
/// Status of a data subject rights request.
/// </summary>
public record DataSubjectRightsStatus(
    string RequestId,
    DataRequestStatus Status,
    DateTime RequestDate,
    DateTime? CompletedDate,
    string? Message
);

/// <summary>
/// Status values for data requests.
/// </summary>
public enum DataRequestStatus
{
    Pending,
    InProgress,
    Completed,
    Failed,
    Cancelled
}

/// <summary>
/// Summary of personal data for a data subject.
/// </summary>
public record PersonalDataSummary(
    string Email,
    bool HasContact,
    bool HasMember,
    int ActivityCount,
    int FormSubmissionCount,
    IEnumerable<string> ConsentAgreements,
    DateTime? FirstActivity,
    DateTime? LastActivity
);

/// <summary>
/// Cookie consent preferences.
/// </summary>
public record CookieConsentPreferences(
    bool NecessaryCookies,
    bool FunctionalCookies,
    bool AnalyticsCookies,
    bool MarketingCookies,
    DateTime? ConsentDate
);
