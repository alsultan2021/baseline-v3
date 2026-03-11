using Baseline.DigitalMarketing.Configuration;
using Baseline.DigitalMarketing.Interfaces;
using CMS.ContactManagement;
using CMS.DataEngine;
using CMS.DataProtection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baseline.DigitalMarketing.Services;

/// <summary>
/// Default implementation of contact tracking service.
/// Uses XbK <see cref="ICurrentContactProvider"/> for contact resolution and
/// <see cref="IConsentAgreementService"/> for consent checks.
/// </summary>
public class ContactTrackingService(
    ICurrentContactProvider currentContactProvider,
    IInfoProvider<ContactInfo> contactInfoProvider,
    IInfoProvider<ConsentInfo> consentInfoProvider,
    IConsentAgreementService consentAgreementService,
    IOptions<BaselineDigitalMarketingOptions> options,
    ILogger<ContactTrackingService> logger) : IContactTrackingService
{
    private readonly ICurrentContactProvider _currentContactProvider = currentContactProvider;
    private readonly IInfoProvider<ContactInfo> _contactInfoProvider = contactInfoProvider;
    private readonly IInfoProvider<ConsentInfo> _consentInfoProvider = consentInfoProvider;
    private readonly IConsentAgreementService _consentAgreementService = consentAgreementService;
    private readonly BaselineDigitalMarketingOptions _options = options.Value;
    private readonly ILogger<ContactTrackingService> _logger = logger;

    // Set once when CDP is detected; avoids repeated NotSupportedException spam.
    private static bool s_cdpDetected;

    /// <inheritdoc />
    public async Task<ContactInfo?> GetCurrentContactAsync()
    {
        if (!_options.EnableContactTracking || s_cdpDetected)
        {
            return null;
        }

        try
        {
            // Check consent if required
            if (_options.RequireConsentBeforeTracking)
            {
                var hasConsent = await HasTrackingConsentAsync();
                if (!hasConsent)
                {
                    _logger.LogDebug("Tracking consent not given, returning null contact");
                    return null;
                }
            }

            // Use GetExistingContact to get without creating new
            var contact = _currentContactProvider.GetExistingContact();
            return contact;
        }
        catch (NotSupportedException)
        {
            HandleCdpDetected();
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current contact");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<ContactInfo> EnsureContactAsync()
    {
        if (!_options.EnableContactTracking)
        {
            throw new InvalidOperationException("Contact tracking is disabled");
        }

        if (s_cdpDetected)
        {
            throw new InvalidOperationException(
                "DefaultCurrentContactProvider is not supported when CDP is enabled");
        }

        try
        {
            // Check consent if required
            if (_options.RequireConsentBeforeTracking)
            {
                var hasConsent = await HasTrackingConsentAsync();
                if (!hasConsent)
                {
                    throw new InvalidOperationException("Tracking consent is required but not given");
                }
            }

            // Use GetCurrentContact which creates if not exists
            var contact = _currentContactProvider.GetCurrentContact();
            if (contact == null)
            {
                throw new InvalidOperationException("Unable to create contact");
            }

            return contact;
        }
        catch (NotSupportedException)
        {
            HandleCdpDetected();
            throw new InvalidOperationException(
                "DefaultCurrentContactProvider is not supported when CDP is enabled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ensuring contact exists");
            throw;
        }
    }

    /// <inheritdoc />
    public Task UpdateContactAsync(ContactInfo contact)
    {
        ArgumentNullException.ThrowIfNull(contact);

        try
        {
            _contactInfoProvider.Set(contact);
            _logger.LogDebug("Updated contact {ContactId}", contact.ContactID);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating contact {ContactId}", contact.ContactID);
            throw;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<bool> HasContactAsync()
    {
        var contact = await GetCurrentContactAsync();
        return contact != null;
    }

    /// <inheritdoc />
    public async Task<string?> GetContactEmailAsync()
    {
        var contact = await GetCurrentContactAsync();
        return contact?.ContactEmail;
    }

    // Static cache so the consent DB lookup + warning happen only once per app lifetime
    // regardless of service lifetime (scoped/transient).
    private static ConsentInfo? s_cachedConsent;
    private static bool s_consentLookedUp;
    private static readonly object s_consentLock = new();

    private async Task<bool> HasTrackingConsentAsync()
    {
        ContactInfo? contact;
        try
        {
            contact = _currentContactProvider.GetExistingContact();
        }
        catch (NotSupportedException)
        {
            HandleCdpDetected();
            return false;
        }

        if (contact == null)
        {
            return false;
        }

        // One-time consent lookup (static) to avoid repeated DB queries and log spam
        if (!s_consentLookedUp)
        {
            lock (s_consentLock)
            {
                if (!s_consentLookedUp)
                {
                    var consentCodeName = _options.TrackingConsentCodeName;
                    s_cachedConsent = _consentInfoProvider
                        .Get()
                        .WhereEquals(nameof(ConsentInfo.ConsentName), consentCodeName)
                        .FirstOrDefault();
                    s_consentLookedUp = true;

                    if (s_cachedConsent == null)
                    {
                        _logger.LogWarning(
                            "Tracking consent '{ConsentCodeName}' not found in the system. "
                            + "Create a consent with this code name or set RequireConsentBeforeTracking=false",
                            consentCodeName);
                    }
                }
            }
        }

        if (s_cachedConsent == null)
        {
            return false;
        }

        return await Task.FromResult(_consentAgreementService.IsAgreed(contact, s_cachedConsent));
    }

    private void HandleCdpDetected()
    {
        if (!s_cdpDetected)
        {
            s_cdpDetected = true;
            _logger.LogWarning(
                "Customer Data Platform is enabled — DefaultCurrentContactProvider is not supported. "
                + "ContactTrackingService will return null contacts. "
                + "Use CDP APIs for contact management instead");
        }
    }
}
