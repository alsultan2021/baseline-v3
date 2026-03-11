using System.Security.Cryptography;
using System.Text;
using Baseline.DataProtection.Interfaces;
using CMS.ContactManagement;
using CMS.DataEngine;
using Microsoft.Extensions.Logging;

namespace Baseline.DataProtection.Services;

/// <summary>
/// Service for data anonymization and pseudonymization.
/// </summary>
/// <param name="contactProvider">Contact info provider.</param>
/// <param name="gdprComplianceService">GDPR compliance service for audit logging.</param>
/// <param name="logger">Logger instance.</param>
public sealed class DataAnonymizationService(
    IInfoProvider<ContactInfo> contactProvider,
    IGdprComplianceService gdprComplianceService,
    ILogger<DataAnonymizationService> logger) : IDataAnonymizationService
{
    private const string AnonymizedPrefix = "ANON_";
    private const string DeletedEmail = "deleted@anonymized.local";

    /// <inheritdoc/>
    public async Task<AnonymizationResult> AnonymizeContactAsync(int contactId)
    {
        logger.LogInformation("Anonymizing contact: {ContactId}", contactId);

        try
        {
            var contact = await contactProvider.GetAsync(contactId);
            if (contact is null)
            {
                return new AnonymizationResult(false, 0, "Contact not found");
            }

            int fieldsAnonymized = AnonymizeContactFields(contact);

            contactProvider.Set(contact);

            await gdprComplianceService.LogDataProcessingActivityAsync(new DataProcessingActivity
            {
                ActivityType = "Anonymization",
                Description = $"Anonymized contact ID {contactId}",
                Purpose = "GDPR Right to Erasure",
                LegalBasis = "LegalObligation",
                DataCategories = ["PersonalData", "ContactInfo"],
                DataSubjectsCount = 1,
                PerformedBy = "System"
            });

            logger.LogInformation(
                "Contact {ContactId} anonymized successfully. Fields anonymized: {FieldsAnonymized}",
                contactId,
                fieldsAnonymized);

            return new AnonymizationResult(true, fieldsAnonymized, "Contact anonymized successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to anonymize contact: {ContactId}", contactId);
            return new AnonymizationResult(false, 0, $"Anonymization failed: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<AnonymizationResult> AnonymizeByEmailAsync(string email)
    {
        logger.LogInformation("Anonymizing contact by email: {Email}", MaskEmail(email));

        try
        {
            var contacts = await contactProvider.Get()
                .WhereEquals(nameof(ContactInfo.ContactEmail), email)
                .GetEnumerableTypedResultAsync();

            var contactList = contacts.ToList();
            if (contactList.Count == 0)
            {
                return new AnonymizationResult(false, 0, "No contacts found with this email");
            }

            int totalFieldsAnonymized = 0;

            foreach (var contact in contactList)
            {
                totalFieldsAnonymized += AnonymizeContactFields(contact);
                contactProvider.Set(contact);
            }

            await gdprComplianceService.LogDataProcessingActivityAsync(new DataProcessingActivity
            {
                ActivityType = "Anonymization",
                Description = $"Anonymized {contactList.Count} contacts by email",
                Purpose = "GDPR Right to Erasure",
                LegalBasis = "LegalObligation",
                DataCategories = ["PersonalData", "ContactInfo"],
                DataSubjectsCount = contactList.Count,
                PerformedBy = "System"
            });

            return new AnonymizationResult(
                true,
                totalFieldsAnonymized,
                $"Anonymized {contactList.Count} contact(s) successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to anonymize contacts by email");
            return new AnonymizationResult(false, 0, $"Anonymization failed: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<PseudonymizationResult> PseudonymizeAsync(string email, string key)
    {
        logger.LogInformation("Pseudonymizing data for: {Email}", MaskEmail(email));

        try
        {
            var contacts = await contactProvider.Get()
                .WhereEquals(nameof(ContactInfo.ContactEmail), email)
                .GetEnumerableTypedResultAsync();

            var contact = contacts.FirstOrDefault();
            if (contact is null)
            {
                return new PseudonymizationResult(false, string.Empty, "Contact not found");
            }

            // Generate pseudonymized ID
            string pseudonymizedId = GeneratePseudonymizedId(email, key);

            // Store mapping (in production, this would be encrypted and stored securely)
            contact.SetValue("ContactNotes", $"PSEUDO:{pseudonymizedId}");
            contact.ContactEmail = $"{pseudonymizedId}@pseudonymized.local";
            contact.ContactFirstName = "Pseudonymized";
            contact.ContactLastName = "User";

            contactProvider.Set(contact);

            await gdprComplianceService.LogDataProcessingActivityAsync(new DataProcessingActivity
            {
                ActivityType = "Pseudonymization",
                Description = "Pseudonymized contact data",
                Purpose = "Data Protection",
                LegalBasis = "Consent",
                DataCategories = ["PersonalData"],
                DataSubjectsCount = 1,
                PerformedBy = "System"
            });

            logger.LogInformation("Contact pseudonymized successfully with ID: {PseudoId}", pseudonymizedId);

            return new PseudonymizationResult(true, pseudonymizedId, "Data pseudonymized successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to pseudonymize data");
            return new PseudonymizationResult(false, string.Empty, $"Pseudonymization failed: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<DepseudonymizationResult> DepseudonymizeAsync(string pseudonymizedId, string key)
    {
        logger.LogInformation("Attempting depseudonymization for: {PseudoId}", pseudonymizedId);

        try
        {
            var contacts = await contactProvider.Get()
                .WhereContains(nameof(ContactInfo.ContactNotes), $"PSEUDO:{pseudonymizedId}")
                .GetEnumerableTypedResultAsync();

            var contact = contacts.FirstOrDefault();
            if (contact is null)
            {
                return new DepseudonymizationResult(false, null, "Pseudonymized record not found");
            }

            // In production, this would decrypt and restore the original email
            // For security, we just confirm the pseudonymized ID matches
            await gdprComplianceService.LogDataProcessingActivityAsync(new DataProcessingActivity
            {
                ActivityType = "Depseudonymization",
                Description = "Depseudonymization request processed",
                Purpose = "Data Access",
                LegalBasis = "Consent",
                DataCategories = ["PersonalData"],
                DataSubjectsCount = 1,
                PerformedBy = "System"
            });

            return new DepseudonymizationResult(
                true,
                null, // In production, would return original email if key matches
                "Depseudonymization successful - contact authorized user for data restoration");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to depseudonymize data");
            return new DepseudonymizationResult(false, null, $"Depseudonymization failed: {ex.Message}");
        }
    }

    private int AnonymizeContactFields(ContactInfo contact)
    {
        int fieldsAnonymized = 0;
        string anonymousId = GenerateAnonymousId();

        if (!string.IsNullOrEmpty(contact.ContactEmail))
        {
            contact.ContactEmail = DeletedEmail;
            fieldsAnonymized++;
        }

        if (!string.IsNullOrEmpty(contact.ContactFirstName))
        {
            contact.ContactFirstName = $"{AnonymizedPrefix}{anonymousId}";
            fieldsAnonymized++;
        }

        if (!string.IsNullOrEmpty(contact.ContactLastName))
        {
            contact.ContactLastName = "User";
            fieldsAnonymized++;
        }

        if (!string.IsNullOrEmpty(contact.ContactMobilePhone))
        {
            contact.ContactMobilePhone = string.Empty;
            fieldsAnonymized++;
        }

        if (!string.IsNullOrEmpty(contact.ContactBusinessPhone))
        {
            contact.ContactBusinessPhone = string.Empty;
            fieldsAnonymized++;
        }

        if (!string.IsNullOrEmpty(contact.ContactAddress1))
        {
            contact.ContactAddress1 = string.Empty;
            fieldsAnonymized++;
        }

        if (!string.IsNullOrEmpty(contact.ContactCity))
        {
            contact.ContactCity = string.Empty;
            fieldsAnonymized++;
        }

        if (!string.IsNullOrEmpty(contact.ContactZIP))
        {
            contact.ContactZIP = string.Empty;
            fieldsAnonymized++;
        }

        if (!string.IsNullOrEmpty(contact.ContactCompanyName))
        {
            contact.ContactCompanyName = string.Empty;
            fieldsAnonymized++;
        }

        // Mark as anonymized
        contact.SetValue("ContactNotes", $"ANONYMIZED:{DateTime.UtcNow:O}");

        return fieldsAnonymized;
    }

    private static string GenerateAnonymousId() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(6))
            .Replace("+", "")
            .Replace("/", "")
            .Replace("=", "")[..8];

    private static string GeneratePseudonymizedId(string email, string key)
    {
        byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{email}:{key}"));
        return Convert.ToHexString(hashBytes)[..16].ToLowerInvariant();
    }

    private static string MaskEmail(string email)
    {
        if (string.IsNullOrEmpty(email) || !email.Contains('@'))
        {
            return "***";
        }

        var parts = email.Split('@');
        var localPart = parts[0];
        var domain = parts[1];

        if (localPart.Length <= 2)
        {
            return $"**@{domain}";
        }

        return $"{localPart[0]}***{localPart[^1]}@{domain}";
    }
}
