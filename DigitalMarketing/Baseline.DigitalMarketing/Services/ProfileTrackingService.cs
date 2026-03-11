using Baseline.DigitalMarketing.Interfaces;
using Baseline.DigitalMarketing.Models;
using CMS.ContactManagement;
using CMS.DataEngine;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baseline.DigitalMarketing.Services;

/// <summary>
/// CDP profile tracking service.
/// Uses <see cref="IInfoProvider{ProfileInfo}"/> for profile queries
/// and <see cref="ICurrentContactProvider"/> for contact ↔ profile resolution.
/// </summary>
public class ProfileTrackingService(
    IInfoProvider<ProfileInfo> profileInfoProvider,
    IInfoProvider<ContactInfo> contactInfoProvider,
    ICurrentContactProvider currentContactProvider,
    IOptions<CustomerDataPlatformOptions> cdpOptions,
    ILogger<ProfileTrackingService> logger) : IProfileTrackingService
{
    private readonly IInfoProvider<ProfileInfo> _profileInfoProvider = profileInfoProvider;
    private readonly IInfoProvider<ContactInfo> _contactInfoProvider = contactInfoProvider;
    private readonly ICurrentContactProvider _currentContactProvider = currentContactProvider;
    private readonly CustomerDataPlatformOptions _cdpOptions = cdpOptions.Value;
    private readonly ILogger<ProfileTrackingService> _logger = logger;

    // Lazy flag — once CDP is confirmed missing we stop trying.
    private static bool s_cdpDisabled;

    /// <inheritdoc />
    public bool IsCdpEnabled()
    {
        if (s_cdpDisabled)
        {
            return false;
        }

        try
        {
            // CustomerDataPlatformOptions.Enabled is set by UseCustomerDataPlatform()
            return _cdpOptions.Enabled;
        }
        catch
        {
            s_cdpDisabled = true;
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<ProfileSummary?> GetCurrentProfileAsync()
    {
        if (!IsCdpEnabled())
        {
            return null;
        }

        try
        {
            var contact = _currentContactProvider.GetExistingContact();
            if (contact == null)
            {
                return null;
            }

            return await ResolveProfileFromContactAsync(contact);
        }
        catch (NotSupportedException)
        {
            _logger.LogDebug("DefaultCurrentContactProvider not supported under CDP");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current profile");
            return null;
        }
    }

    /// <inheritdoc />
    public Task<ProfileSummary?> GetProfileByIdAsync(int profileId)
    {
        try
        {
            var profile = _profileInfoProvider.Get(profileId);
            return Task.FromResult(profile != null ? MapToSummary(profile) : null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting profile {ProfileId}", profileId);
            return Task.FromResult<ProfileSummary?>(null);
        }
    }

    /// <inheritdoc />
    public Task<ProfileSummary?> GetProfileByGuidAsync(Guid profileGuid)
    {
        try
        {
            var profile = _profileInfoProvider.Get()
                .WhereEquals(nameof(ProfileInfo.ProfileGuid), profileGuid)
                .FirstOrDefault();
            return Task.FromResult(profile != null ? MapToSummary(profile) : null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting profile {ProfileGuid}", profileGuid);
            return Task.FromResult<ProfileSummary?>(null);
        }
    }

    /// <inheritdoc />
    public async Task<ProfileTrackingResult> GetTrackingStatusAsync()
    {
        if (!IsCdpEnabled())
        {
            return new ProfileTrackingResult(false, null, false);
        }

        var profile = await GetCurrentProfileAsync();
        return new ProfileTrackingResult(
            IsTracked: profile != null,
            Profile: profile,
            HasTrackingConsent: profile != null
        );
    }

    /// <inheritdoc />
    public Task UpdateProfileDisplayNameAsync(int profileId, string displayName)
    {
        try
        {
            var profile = _profileInfoProvider.Get(profileId);
            if (profile != null)
            {
                profile.ProfileDisplayName = displayName;
                _profileInfoProvider.Set(profile);
                _logger.LogDebug("Updated profile {ProfileId} display name", profileId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile {ProfileId}", profileId);
        }

        return Task.CompletedTask;
    }

    // ── private helpers ────────────────────────────────────────

    private Task<ProfileSummary?> ResolveProfileFromContactAsync(ContactInfo contact)
    {
        // ProfileReferenceInfo links Contact ↔ Profile
        var profileRef = ProfileReferenceInfo.Provider.Get()
            .WhereEquals("ProfileReferenceContactID", contact.ContactID)
            .FirstOrDefault();

        if (profileRef == null)
        {
            return Task.FromResult<ProfileSummary?>(null);
        }

        var profile = _profileInfoProvider.Get(profileRef.GetIntegerValue("ProfileReferenceProfileID", 0));
        if (profile == null)
        {
            return Task.FromResult<ProfileSummary?>(null);
        }

        return Task.FromResult<ProfileSummary?>(MapToSummary(profile, contact));
    }

    private static ProfileSummary MapToSummary(ProfileInfo profile, ContactInfo? contact = null) =>
        new(
            ProfileId: profile.ProfileID,
            ProfileName: profile.ProfileName,
            ProfileGuid: profile.ProfileGuid,
            DisplayName: profile.ProfileDisplayName ?? "",
            IsVerified: profile.ProfileIsVerified,
            Email: contact?.ContactEmail,
            FirstName: contact?.ContactFirstName,
            LastName: contact?.ContactLastName,
            MemberId: null,  // resolved via ProfileContext if available
            ContactId: contact?.ContactID
        );
}
