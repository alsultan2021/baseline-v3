using Baseline.DigitalMarketing.Interfaces;
using Baseline.DigitalMarketing.Models;
using CMS.DataProtection;
using CMS.Helpers;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.CustomerDataPlatform.Web.Mvc;
using Kentico.Web.Mvc;
using Microsoft.Extensions.Logging;

namespace Baseline.DigitalMarketing.Services;

/// <summary>
/// CDP profile consent service.
/// Wraps <see cref="IProfileConsentAgreementService"/> for profile-based
/// consent management (replaces traditional contact-based consent under CDP).
/// </summary>
public class ProfileConsentService(
    IProfileConsentAgreementService profileConsentAgreementService,
    ICurrentCookieLevelProvider currentCookieLevelProvider,
    IPreferredLanguageRetriever preferredLanguageRetriever,
    ILogger<ProfileConsentService> logger) : IProfileConsentService
{
    private readonly IProfileConsentAgreementService _profileConsentAgreementService = profileConsentAgreementService;
    private readonly ICurrentCookieLevelProvider _currentCookieLevelProvider = currentCookieLevelProvider;
    private readonly IPreferredLanguageRetriever _preferredLanguageRetriever = preferredLanguageRetriever;
    private readonly ILogger<ProfileConsentService> _logger = logger;

    /// <inheritdoc />
    public async Task<bool> IsAgreedAsync(string consentCodeName, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _profileConsentAgreementService.IsAgreed(consentCodeName, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking profile consent {ConsentCodeName}", consentCodeName);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<ConsentActionResult> AgreeAsync(string consentCodeName, CancellationToken cancellationToken = default)
    {
        try
        {
            // Raise cookie level so tracking scripts are enabled
            _currentCookieLevelProvider.SetCurrentCookieLevel(Kentico.Web.Mvc.CookieLevel.All.Level);

            bool success = await _profileConsentAgreementService.TryAgree(consentCodeName, cancellationToken);
            if (success)
            {
                _logger.LogDebug("Profile agreed to consent {ConsentCodeName}", consentCodeName);
            }

            return new ConsentActionResult(success,
                success ? null : "Consent agreement failed — profile may not be tracked");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error agreeing to consent {ConsentCodeName}", consentCodeName);
            return new ConsentActionResult(false, ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<ConsentActionResult> RevokeAsync(string consentCodeName, CancellationToken cancellationToken = default)
    {
        try
        {
            bool success = await _profileConsentAgreementService.TryRevoke(consentCodeName, cancellationToken);
            if (success)
            {
                // Lower cookie level to disable tracking
                _currentCookieLevelProvider.SetCurrentCookieLevel(Kentico.Web.Mvc.CookieLevel.System.Level);
                _logger.LogDebug("Profile revoked consent {ConsentCodeName}", consentCodeName);
            }

            return new ConsentActionResult(success,
                success ? null : "Consent revocation failed — profile may not be tracked");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking consent {ConsentCodeName}", consentCodeName);
            return new ConsentActionResult(false, ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ProfileConsentAgreement>> GetAgreedConsentsAsync(
        string? languageName = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var lang = languageName ?? _preferredLanguageRetriever.Get();
            var agreedConsents = await _profileConsentAgreementService.GetAgreedConsents(cancellationToken);

            var results = agreedConsents.Select(c =>
            {
                var text = c.GetConsentText(lang);
                return new ProfileConsentAgreement(
                    ConsentCodeName: c.Name,
                    ConsentDisplayName: c.DisplayName,
                    IsAgreed: true,
                    ShortText: text.ShortText,
                    FullText: text.FullText
                );
            });

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting agreed consents");
            return Enumerable.Empty<ProfileConsentAgreement>();
        }
    }

}
