using System.Collections.Concurrent;
using Baseline.DataProtection.Configuration;
using Baseline.DataProtection.Interfaces;
using CMS.ContactManagement;
using CMS.DataEngine;
using CMS.DataProtection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baseline.DataProtection.Services;

/// <summary>
/// Service for GDPR compliance auditing and reporting.
/// </summary>
/// <param name="options">Data protection options.</param>
/// <param name="consentProvider">Consent info provider.</param>
/// <param name="consentAgreementService">Consent agreement service.</param>
/// <param name="logger">Logger instance.</param>
#pragma warning disable CS9113 // Parameter is unread - reserved for future use
public sealed class GdprComplianceService(
    IOptions<BaselineDataProtectionOptions> options,
    IInfoProvider<ConsentInfo> consentProvider,
    IConsentAgreementService consentAgreementService,
    ILogger<GdprComplianceService> logger) : IGdprComplianceService
#pragma warning restore CS9113
{
    private readonly BaselineDataProtectionOptions _options = options.Value;
    private readonly ConcurrentQueue<DataProcessingActivity> _auditLog = new();
    private const int MaxAuditLogSize = 10000;

    /// <inheritdoc/>
    public async Task<GdprComplianceReport> GenerateComplianceReportAsync()
    {
        logger.LogDebug("Generating GDPR compliance report");

        var status = await CheckComplianceAsync();
        var issues = await GetComplianceIssuesAsync();
        var consents = await GetConsentsCountAsync();

        var report = new GdprComplianceReport
        {
            Status = status,
            TotalConsents = consents,
            ActiveConsentAgreements = await GetActiveAgreementsCountAsync(),
            Issues = issues.ToList(),
            DataProcessing = GetDataProcessingSummary(),
            RetentionStatus = new DataRetentionStatus
            {
                ActivePolicies = 0, // Would be populated from actual policy store
                PendingRecords = 0
            }
        };

        await LogDataProcessingActivityAsync(new DataProcessingActivity
        {
            ActivityType = "ComplianceReport",
            Description = "Generated GDPR compliance report",
            Purpose = "Compliance monitoring",
            LegalBasis = "LegitimateInterest"
        });

        logger.LogInformation("GDPR compliance report generated with status: {Status}", status);
        return report;
    }

    /// <inheritdoc/>
    public async Task<GdprComplianceStatus> CheckComplianceAsync()
    {
        var issues = await GetComplianceIssuesAsync();
        var issuesList = issues.ToList();

        if (issuesList.Any(i => i.Severity == ComplianceIssueSeverity.Critical ||
                                i.Severity == ComplianceIssueSeverity.Error))
        {
            return GdprComplianceStatus.NonCompliant;
        }

        if (issuesList.Any(i => i.Severity == ComplianceIssueSeverity.Warning))
        {
            return GdprComplianceStatus.Warning;
        }

        return GdprComplianceStatus.Compliant;
    }

    /// <inheritdoc/>
    public Task<IEnumerable<ComplianceIssue>> GetComplianceIssuesAsync()
    {
        var issues = new List<ComplianceIssue>();

        // Check consent configuration
        if (!_options.ShowConsentBanner)
        {
            issues.Add(new ComplianceIssue
            {
                Code = "CONSENT-001",
                Severity = ComplianceIssueSeverity.Warning,
                Description = "Consent banner is disabled",
                Remediation = "Enable consent banner to ensure users can provide explicit consent",
                GdprArticle = "Article 7 - Conditions for consent"
            });
        }

        if (!_options.ShowRejectButton)
        {
            issues.Add(new ComplianceIssue
            {
                Code = "CONSENT-002",
                Severity = ComplianceIssueSeverity.Error,
                Description = "Reject button is not shown in consent banner",
                Remediation = "Enable reject button to allow users to decline cookies easily",
                GdprArticle = "Article 7 - Conditions for consent"
            });
        }

        if (string.IsNullOrEmpty(_options.PrivacyPolicyUrl))
        {
            issues.Add(new ComplianceIssue
            {
                Code = "POLICY-001",
                Severity = ComplianceIssueSeverity.Critical,
                Description = "Privacy policy URL is not configured",
                Remediation = "Configure a privacy policy URL to inform users about data processing",
                GdprArticle = "Article 13 - Information to be provided"
            });
        }

        if (string.IsNullOrEmpty(_options.CookiePolicyUrl))
        {
            issues.Add(new ComplianceIssue
            {
                Code = "POLICY-002",
                Severity = ComplianceIssueSeverity.Warning,
                Description = "Cookie policy URL is not configured",
                Remediation = "Configure a cookie policy URL to inform users about cookie usage",
                GdprArticle = "ePrivacy Directive"
            });
        }

        if (!_options.EnableDataExport)
        {
            issues.Add(new ComplianceIssue
            {
                Code = "RIGHTS-001",
                Severity = ComplianceIssueSeverity.Error,
                Description = "Data export functionality is disabled",
                Remediation = "Enable data export to support the right to data portability",
                GdprArticle = "Article 20 - Right to data portability"
            });
        }

        if (!_options.EnableDataErasure)
        {
            issues.Add(new ComplianceIssue
            {
                Code = "RIGHTS-002",
                Severity = ComplianceIssueSeverity.Critical,
                Description = "Data erasure functionality is disabled",
                Remediation = "Enable data erasure to support the right to be forgotten",
                GdprArticle = "Article 17 - Right to erasure"
            });
        }

        if (_options.DataErasureDeadlineDays > 30)
        {
            issues.Add(new ComplianceIssue
            {
                Code = "RIGHTS-003",
                Severity = ComplianceIssueSeverity.Warning,
                Description = $"Data erasure deadline ({_options.DataErasureDeadlineDays} days) exceeds GDPR requirement",
                Remediation = "Reduce data erasure deadline to 30 days or less per GDPR requirements",
                GdprArticle = "Article 17 - Right to erasure"
            });
        }

        if (_options.ConsentCookieExpirationDays > 365)
        {
            issues.Add(new ComplianceIssue
            {
                Code = "CONSENT-003",
                Severity = ComplianceIssueSeverity.Info,
                Description = "Consent cookie expiration exceeds 1 year",
                Remediation = "Consider reducing consent cookie expiration to 12 months",
                GdprArticle = "Article 7 - Conditions for consent"
            });
        }

        return Task.FromResult<IEnumerable<ComplianceIssue>>(issues);
    }

    /// <inheritdoc/>
    public Task LogDataProcessingActivityAsync(DataProcessingActivity activity)
    {
        // Maintain max size
        while (_auditLog.Count >= MaxAuditLogSize && _auditLog.TryDequeue(out _))
        {
        }

        _auditLog.Enqueue(activity);

        logger.LogDebug(
            "Logged data processing activity: {ActivityType} - {Description}",
            activity.ActivityType,
            activity.Description);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IEnumerable<DataProcessingActivity>> GetAuditLogAsync(
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        int limit = 100)
    {
        IEnumerable<DataProcessingActivity> activities = _auditLog.ToArray();

        if (from.HasValue)
        {
            activities = activities.Where(a => a.Timestamp >= from.Value);
        }

        if (to.HasValue)
        {
            activities = activities.Where(a => a.Timestamp <= to.Value);
        }

        return Task.FromResult(activities.OrderByDescending(a => a.Timestamp).Take(limit));
    }

    private async Task<int> GetConsentsCountAsync()
    {
        var consents = await consentProvider.Get().GetEnumerableTypedResultAsync();
        return consents.Count();
    }

    private Task<int> GetActiveAgreementsCountAsync()
    {
        // This would need actual implementation based on consent agreement data
        // For now returning 0 as placeholder
        return Task.FromResult(0);
    }

    private DataProcessingSummary GetDataProcessingSummary()
    {
        var activities = _auditLog.ToArray();
        var earliest = activities.MinBy(a => a.Timestamp)?.Timestamp;
        var latest = activities.MaxBy(a => a.Timestamp)?.Timestamp;

        return new DataProcessingSummary
        {
            TotalActivities = activities.Length,
            ByType = activities.GroupBy(a => a.ActivityType)
                              .ToDictionary(g => g.Key, g => g.Count()),
            FromDate = earliest,
            ToDate = latest
        };
    }
}
