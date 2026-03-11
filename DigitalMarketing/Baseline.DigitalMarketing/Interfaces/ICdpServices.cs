using Baseline.DigitalMarketing.Models;
using CMS.ContactManagement;

namespace Baseline.DigitalMarketing.Interfaces;

/// <summary>
/// Service for working with CDP profiles.
/// Wraps XbK's <c>ProfileInfo</c>, <c>IInfoProvider&lt;ProfileInfo&gt;</c>,
/// and contact-to-profile resolution.
/// </summary>
public interface IProfileTrackingService
{
    /// <summary>
    /// Gets the current visitor's profile summary, or null when CDP
    /// is disabled or the visitor is not tracked.
    /// </summary>
    Task<ProfileSummary?> GetCurrentProfileAsync();

    /// <summary>
    /// Gets a profile by its database ID.
    /// </summary>
    Task<ProfileSummary?> GetProfileByIdAsync(int profileId);

    /// <summary>
    /// Gets a profile by its GUID.
    /// </summary>
    Task<ProfileSummary?> GetProfileByGuidAsync(Guid profileGuid);

    /// <summary>
    /// Checks whether the current visitor is being tracked as a profile.
    /// </summary>
    Task<ProfileTrackingResult> GetTrackingStatusAsync();

    /// <summary>
    /// Updates the display name of the specified profile.
    /// </summary>
    Task UpdateProfileDisplayNameAsync(int profileId, string displayName);

    /// <summary>
    /// Checks whether CDP is enabled in this Xperience instance.
    /// </summary>
    bool IsCdpEnabled();
}

/// <summary>
/// Service for managing consent agreements on CDP profiles.
/// Wraps XbK's <see cref="Kentico.CustomerDataPlatform.Web.Mvc.IProfileConsentAgreementService"/>.
/// </summary>
public interface IProfileConsentService
{
    /// <summary>
    /// Checks whether the current profile has agreed to the specified consent.
    /// </summary>
    Task<bool> IsAgreedAsync(string consentCodeName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records an agreement with the specified consent for the current profile.
    /// </summary>
    Task<ConsentActionResult> AgreeAsync(string consentCodeName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes the agreement with the specified consent for the current profile.
    /// </summary>
    Task<ConsentActionResult> RevokeAsync(string consentCodeName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all consents the current profile has agreed to.
    /// </summary>
    Task<IEnumerable<ProfileConsentAgreement>> GetAgreedConsentsAsync(
        string? languageName = null,
        CancellationToken cancellationToken = default);
}
