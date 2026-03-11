namespace Baseline.DataProtection.Models;

/// <summary>
/// Consent banner display configuration.
/// </summary>
public record ConsentBannerConfig(
    string Title,
    string Description,
    string AcceptAllText,
    string RejectAllText,
    string CustomizeText,
    string SavePreferencesText,
    ConsentBannerPosition Position,
    bool ShowRejectButton,
    bool ShowCustomizeButton,
    string? PrivacyPolicyUrl,
    string? CookiePolicyUrl
);

/// <summary>
/// Position of the consent banner.
/// </summary>
public enum ConsentBannerPosition
{
    Top,
    Bottom,
    Center,
    BottomLeft,
    BottomRight
}

/// <summary>
/// Consent information for display.
/// </summary>
public record ConsentDisplayInfo(
    string CodeName,
    string DisplayName,
    string ShortText,
    string FullText,
    bool IsRequired,
    bool IsAgreed
);

/// <summary>
/// Cookie information for display in preferences.
/// </summary>
public record CookieInfo(
    string Name,
    string Description,
    CookieCategoryInfo Category,
    string Duration,
    string? Provider,
    bool IsThirdParty
);

/// <summary>
/// Cookie category information.
/// </summary>
public record CookieCategoryInfo(
    string Name,
    string Description,
    bool IsRequired,
    IEnumerable<CookieInfo>? Cookies
);

/// <summary>
/// Data export request.
/// </summary>
public record DataExportRequest(
    string Email,
    string? Format,
    bool IncludeActivities,
    bool IncludeFormSubmissions,
    bool IncludeConsentHistory
);

/// <summary>
/// Data erasure request.
/// </summary>
public record DataErasureRequest(
    string Email,
    string? VerificationCode,
    bool ConfirmErasure,
    string? Reason
);

/// <summary>
/// Consent agreement history entry.
/// </summary>
public record ConsentHistoryEntry(
    string ConsentCodeName,
    string ConsentDisplayName,
    bool WasAgreed,
    DateTime Timestamp,
    string? IpAddress,
    string? UserAgent
);

/// <summary>
/// Result of consent operation.
/// </summary>
public record ConsentOperationResult(
    bool Success,
    string? Message,
    string? ErrorCode
);

/// <summary>
/// Cookie consent cookie data.
/// </summary>
public record CookieConsentCookieData(
    bool Necessary,
    bool Functional,
    bool Analytics,
    bool Marketing,
    long Timestamp,
    string? Version
);
