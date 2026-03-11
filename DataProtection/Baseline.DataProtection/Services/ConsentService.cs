using Baseline.DataProtection.Configuration;
using Baseline.DataProtection.Interfaces;
using Baseline.DigitalMarketing.Interfaces;
using CMS.ContactManagement;
using CMS.DataEngine;
using CMS.DataProtection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baseline.DataProtection.Services;

/// <summary>
/// Default implementation of consent service.
/// </summary>
public class ConsentService : IConsentService
{
    private readonly IInfoProvider<ConsentInfo> _consentInfoProvider;
    private readonly IConsentAgreementService _consentAgreementService;
    private readonly IContactTrackingService _contactTrackingService;
    private readonly IOptions<BaselineDataProtectionOptions> _options;
    private readonly ILogger<ConsentService> _logger;

    public ConsentService(
        IInfoProvider<ConsentInfo> consentInfoProvider,
        IConsentAgreementService consentAgreementService,
        IContactTrackingService contactTrackingService,
        IOptions<BaselineDataProtectionOptions> options,
        ILogger<ConsentService> logger)
    {
        _consentInfoProvider = consentInfoProvider;
        _consentAgreementService = consentAgreementService;
        _contactTrackingService = contactTrackingService;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ConsentInfo>> GetAvailableConsentsAsync()
    {
        var consents = _consentInfoProvider.Get().ToList();
        return await Task.FromResult(consents);
    }

    /// <inheritdoc />
    public async Task<ConsentInfo?> GetConsentAsync(string consentCodeName)
    {
        var consent = _consentInfoProvider.Get()
            .WhereEquals(nameof(ConsentInfo.ConsentName), consentCodeName)
            .FirstOrDefault();
        
        return await Task.FromResult(consent);
    }

    /// <inheritdoc />
    public async Task<bool> HasConsentAsync(string consentCodeName)
    {
        try
        {
            var contact = await _contactTrackingService.GetCurrentContactAsync();
            if (contact == null)
            {
                return false;
            }

            var consent = await GetConsentAsync(consentCodeName);
            if (consent == null)
            {
                return false;
            }

            return _consentAgreementService.IsAgreed(contact, consent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking consent {ConsentCodeName}", consentCodeName);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task AgreeToConsentAsync(string consentCodeName)
    {
        try
        {
            var contact = await _contactTrackingService.EnsureContactAsync();
            var consent = await GetConsentAsync(consentCodeName);
            
            if (consent == null)
            {
                _logger.LogWarning("Consent {ConsentCodeName} not found", consentCodeName);
                return;
            }

            _consentAgreementService.Agree(contact, consent);
            _logger.LogInformation("Contact {ContactId} agreed to consent {ConsentCodeName}", 
                contact.ContactID, consentCodeName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording consent agreement for {ConsentCodeName}", consentCodeName);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task RevokeConsentAsync(string consentCodeName)
    {
        try
        {
            var contact = await _contactTrackingService.GetCurrentContactAsync();
            if (contact == null)
            {
                _logger.LogWarning("No contact found to revoke consent");
                return;
            }

            var consent = await GetConsentAsync(consentCodeName);
            if (consent == null)
            {
                _logger.LogWarning("Consent {ConsentCodeName} not found", consentCodeName);
                return;
            }

            _consentAgreementService.Revoke(contact, consent);
            _logger.LogInformation("Contact {ContactId} revoked consent {ConsentCodeName}", 
                contact.ContactID, consentCodeName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking consent {ConsentCodeName}", consentCodeName);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> GetAgreedConsentsAsync()
    {
        try
        {
            var contact = await _contactTrackingService.GetCurrentContactAsync();
            if (contact == null)
            {
                return Enumerable.Empty<string>();
            }

            var allConsents = await GetAvailableConsentsAsync();
            var agreedConsents = new List<string>();

            foreach (var consent in allConsents)
            {
                if (_consentAgreementService.IsAgreed(contact, consent))
                {
                    agreedConsents.Add(consent.ConsentName);
                }
            }

            return agreedConsents;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting agreed consents");
            return Enumerable.Empty<string>();
        }
    }

    /// <inheritdoc />
    public async Task<string?> GetConsentTextAsync(string consentCodeName, string? languageCode = null)
    {
        var consent = await GetConsentAsync(consentCodeName);
        if (consent == null)
        {
            return null;
        }

        // Return the consent content - in a real implementation, you would
        // use language-specific content based on the languageCode parameter
        return consent.ConsentContent;
    }
}
