using Baseline.DigitalMarketing.Interfaces;
using Baseline.DigitalMarketing.Models;
using CMS.ContactManagement;

namespace Baseline.DigitalMarketing.Configuration;

/// <summary>
/// Configuration options for the Baseline Digital Marketing module.
/// </summary>
public class BaselineDigitalMarketingOptions
{
    /// <summary>
    /// Enable contact tracking for website visitors. Default: true.
    /// </summary>
    public bool EnableContactTracking { get; set; } = true;

    /// <summary>
    /// Enable activity logging for tracked contacts. Default: true.
    /// </summary>
    public bool EnableActivityLogging { get; set; } = true;

    /// <summary>
    /// Require consent before tracking visitors. Default: true (GDPR compliant).
    /// </summary>
    public bool RequireConsentBeforeTracking { get; set; } = true;

    /// <summary>
    /// The consent code name required for tracking. Default: "tracking".
    /// </summary>
    public string TrackingConsentCodeName { get; set; } = "tracking";

    /// <summary>
    /// Enable automatic page visit activity logging. Default: true.
    /// </summary>
    public bool LogPageVisitActivities { get; set; } = true;

    /// <summary>
    /// Custom activity types to register on application startup.
    /// </summary>
    public List<CustomActivityTypeRegistration> CustomActivityTypes { get; set; } = new();

    /// <summary>
    /// Mappings from form field names to contact field names.
    /// Key: Form field code name, Value: Contact field code name.
    /// </summary>
    public Dictionary<string, string> ContactFieldMappings { get; set; } = new()
    {
        ["email"] = "ContactEmail",
        ["firstname"] = "ContactFirstName",
        ["lastname"] = "ContactLastName",
        ["phone"] = "ContactMobilePhone",
        ["company"] = "ContactCompanyName"
    };

    /// <summary>
    /// Cache duration for contact group membership checks in minutes. Default: 5.
    /// </summary>
    public int ContactGroupCacheDurationMinutes { get; set; } = 5;

    /// <summary>
    /// Enable debug mode for personalization (logs matched conditions). Default: false.
    /// </summary>
    public bool EnablePersonalizationDebugMode { get; set; } = false;

    /// <summary>
    /// Activity types that should not be logged (excluded from tracking).
    /// </summary>
    public HashSet<string> ExcludedActivityTypes { get; set; } = new();

    /// <summary>
    /// URL paths that should be excluded from page visit tracking.
    /// </summary>
    public HashSet<string> ExcludedPagePaths { get; set; } = new()
    {
        "/api/",
        "/admin/",
        "/_content/",
        "/health"
    };

    // ── CDP (Customer Data Platform) options ────────────────────

    /// <summary>
    /// Enable CDP profile-based tracking services. Default: false.
    /// When true, <see cref="IProfileTrackingService"/> and
    /// <see cref="IProfileConsentService"/> are registered.
    /// Requires <c>UseCustomerDataPlatform()</c> in the application builder.
    /// </summary>
    public bool EnableCdpProfileTracking { get; set; } = false;

    /// <summary>
    /// Consent code name used for CDP tracking consent. Default: "TrackingConsent".
    /// </summary>
    public string CdpTrackingConsentCodeName { get; set; } = "TrackingConsent";

    /// <summary>
    /// Options for the CDP member registration handler
    /// (<see cref="CustomerDataPlatformOptions.MemberEvents.OnRegistered"/>).
    /// </summary>
    public MemberRegistrationOptions CdpMemberRegistration { get; set; } = new();
}
