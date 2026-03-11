using Baseline.DataProtection.Models;

namespace Baseline.DataProtection.Configuration;

/// <summary>
/// Configuration options for the Baseline Data Protection module.
/// </summary>
public class BaselineDataProtectionOptions
{
    /// <summary>
    /// Whether consent is required before tracking visitors. Default: true (GDPR compliant).
    /// </summary>
    public bool RequireConsentBeforeTracking { get; set; } = true;

    /// <summary>
    /// Position of the cookie consent banner. Default: Bottom.
    /// </summary>
    public ConsentBannerPosition BannerPosition { get; set; } = ConsentBannerPosition.Bottom;

    /// <summary>
    /// Days to store the consent cookie. Default: 365.
    /// </summary>
    public int ConsentCookieExpirationDays { get; set; } = 365;

    /// <summary>
    /// Name of the consent cookie. Default: "cookie_consent".
    /// </summary>
    public string ConsentCookieName { get; set; } = "cookie_consent";

    /// <summary>
    /// Code names of consents required for tracking. Default: ["tracking"].
    /// </summary>
    public List<string> RequiredConsentsForTracking { get; set; } = new() { "tracking" };

    /// <summary>
    /// Enable automatic consent banner display. Default: true.
    /// </summary>
    public bool ShowConsentBanner { get; set; } = true;

    /// <summary>
    /// Show a "Reject All" button on the consent banner. Default: true.
    /// </summary>
    public bool ShowRejectButton { get; set; } = true;

    /// <summary>
    /// Show a "Customize" button for granular cookie preferences. Default: true.
    /// </summary>
    public bool ShowCustomizeButton { get; set; } = true;

    /// <summary>
    /// URL to the privacy policy page. Optional.
    /// </summary>
    public string? PrivacyPolicyUrl { get; set; }

    /// <summary>
    /// URL to the cookie policy page. Optional.
    /// </summary>
    public string? CookiePolicyUrl { get; set; }

    /// <summary>
    /// Enable data export functionality. Default: true.
    /// </summary>
    public bool EnableDataExport { get; set; } = true;

    /// <summary>
    /// Enable data erasure requests. Default: true.
    /// </summary>
    public bool EnableDataErasure { get; set; } = true;

    /// <summary>
    /// Require email verification for data requests. Default: true.
    /// </summary>
    public bool RequireEmailVerification { get; set; } = true;

    /// <summary>
    /// Days to complete data erasure requests. Default: 30 (GDPR requirement).
    /// </summary>
    public int DataErasureDeadlineDays { get; set; } = 30;

    /// <summary>
    /// Format options for data export. Default: JSON, CSV.
    /// </summary>
    public List<string> DataExportFormats { get; set; } = new() { "json", "csv" };

    /// <summary>
    /// Banner text configuration.
    /// </summary>
    public ConsentBannerTextOptions BannerText { get; set; } = new();

    /// <summary>
    /// Cookie categories configuration.
    /// </summary>
    public List<CookieCategoryConfig> CookieCategories { get; set; } = new()
    {
        new CookieCategoryConfig
        {
            Name = "Necessary",
            Description = "These cookies are essential for the website to function properly.",
            IsRequired = true
        },
        new CookieCategoryConfig
        {
            Name = "Functional",
            Description = "These cookies enable enhanced functionality and personalization."
        },
        new CookieCategoryConfig
        {
            Name = "Analytics",
            Description = "These cookies help us understand how visitors interact with our website."
        },
        new CookieCategoryConfig
        {
            Name = "Marketing",
            Description = "These cookies are used to deliver relevant advertisements."
        }
    };

    /// <summary>
    /// Enable GDPR compliance monitoring and reporting. Default: true.
    /// </summary>
    public bool EnableComplianceMonitoring { get; set; } = true;

    /// <summary>
    /// Enable data retention policy enforcement. Default: false.
    /// </summary>
    public bool EnableDataRetentionPolicies { get; set; } = false;

    /// <summary>
    /// Enable audit logging for data processing activities. Default: true.
    /// </summary>
    public bool EnableAuditLogging { get; set; } = true;

    /// <summary>
    /// Data Processing Officer (DPO) email for notifications. Optional.
    /// </summary>
    public string? DataProtectionOfficerEmail { get; set; }

    /// <summary>
    /// Enable anonymization as alternative to deletion. Default: true.
    /// </summary>
    public bool AllowAnonymizationAsAlternative { get; set; } = true;

    /// <summary>
    /// Days to keep audit logs. Default: 365.
    /// </summary>
    public int AuditLogRetentionDays { get; set; } = 365;

    /// <summary>
    /// Interval in hours between automatic retention policy runs. Default: 24.
    /// </summary>
    public int RetentionPolicyRunIntervalHours { get; set; } = 24;

    /// <summary>
    /// Enable notification emails for data subject requests. Default: true.
    /// </summary>
    public bool EnableDataRequestNotifications { get; set; } = true;

    /// <summary>
    /// GDPR Article 30 - Record of Processing Activities name.
    /// </summary>
    public string? ProcessingActivityRecordName { get; set; }
}

/// <summary>
/// Text options for the consent banner.
/// </summary>
public class ConsentBannerTextOptions
{
    public string Title { get; set; } = "Cookie Consent";
    public string Description { get; set; } = "We use cookies to enhance your browsing experience, analyze site traffic, and personalize content. By clicking 'Accept All', you consent to our use of cookies.";
    public string AcceptAllText { get; set; } = "Accept All";
    public string RejectAllText { get; set; } = "Reject All";
    public string CustomizeText { get; set; } = "Customize";
    public string SavePreferencesText { get; set; } = "Save Preferences";
}

/// <summary>
/// Configuration for a cookie category.
/// </summary>
public class CookieCategoryConfig
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public List<CookieConfig>? Cookies { get; set; }
}

/// <summary>
/// Configuration for a specific cookie.
/// </summary>
public class CookieConfig
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public string? Provider { get; set; }
    public bool IsThirdParty { get; set; }
}
