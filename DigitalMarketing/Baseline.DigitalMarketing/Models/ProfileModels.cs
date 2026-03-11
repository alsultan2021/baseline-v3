using CMS.ContactManagement;
using CMS.Membership;

namespace Baseline.DigitalMarketing.Models;

/// <summary>
/// Summary of a CDP profile.
/// </summary>
public record ProfileSummary(
    int ProfileId,
    string ProfileName,
    Guid ProfileGuid,
    string DisplayName,
    bool IsVerified,
    string? Email,
    string? FirstName,
    string? LastName,
    int? MemberId,
    int? ContactId
);

/// <summary>
/// Result of a profile tracking check.
/// </summary>
public record ProfileTrackingResult(
    bool IsTracked,
    ProfileSummary? Profile,
    bool HasTrackingConsent
);

/// <summary>
/// Represents a consent agreement for a profile.
/// </summary>
public record ProfileConsentAgreement(
    string ConsentCodeName,
    string ConsentDisplayName,
    bool IsAgreed,
    string? ShortText,
    string? FullText
);

/// <summary>
/// Options for the member registration CDP handler.
/// </summary>
public record MemberRegistrationOptions(
    bool SetProfileAsVerified = true,
    Func<MemberInfo, string>? DisplayNameResolver = null
);

/// <summary>
/// Result of agreeing to or revoking a consent.
/// </summary>
public record ConsentActionResult(
    bool Success,
    string? ErrorMessage = null
);
