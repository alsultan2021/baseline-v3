namespace Baseline.DataProtection.Interfaces;

/// <summary>
/// Service for GDPR compliance auditing and reporting.
/// </summary>
public interface IGdprComplianceService
{
    /// <summary>
    /// Generates a GDPR compliance report.
    /// </summary>
    Task<GdprComplianceReport> GenerateComplianceReportAsync();

    /// <summary>
    /// Checks if the system is GDPR compliant.
    /// </summary>
    Task<GdprComplianceStatus> CheckComplianceAsync();

    /// <summary>
    /// Gets a list of compliance violations or warnings.
    /// </summary>
    Task<IEnumerable<ComplianceIssue>> GetComplianceIssuesAsync();

    /// <summary>
    /// Logs a data processing activity for audit purposes.
    /// </summary>
    Task LogDataProcessingActivityAsync(DataProcessingActivity activity);

    /// <summary>
    /// Gets the data processing audit log.
    /// </summary>
    Task<IEnumerable<DataProcessingActivity>> GetAuditLogAsync(
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        int limit = 100);
}

/// <summary>
/// Service for managing data retention policies.
/// </summary>
public interface IDataRetentionService
{
    /// <summary>
    /// Gets all configured retention policies.
    /// </summary>
    Task<IEnumerable<DataRetentionPolicy>> GetPoliciesAsync();

    /// <summary>
    /// Gets a specific retention policy by name.
    /// </summary>
    Task<DataRetentionPolicy?> GetPolicyAsync(string policyName);

    /// <summary>
    /// Executes data retention cleanup based on policies.
    /// </summary>
    Task<DataRetentionResult> ExecuteRetentionPoliciesAsync();

    /// <summary>
    /// Gets data that would be affected by retention policies (preview).
    /// </summary>
    Task<DataRetentionPreview> PreviewRetentionAsync();

    /// <summary>
    /// Schedules retention cleanup at specified intervals.
    /// </summary>
    Task ScheduleRetentionAsync(TimeSpan interval);
}

/// <summary>
/// Service for data anonymization.
/// </summary>
public interface IDataAnonymizationService
{
    /// <summary>
    /// Anonymizes personal data for a contact.
    /// </summary>
    Task<AnonymizationResult> AnonymizeContactAsync(int contactId);

    /// <summary>
    /// Anonymizes personal data by email.
    /// </summary>
    Task<AnonymizationResult> AnonymizeByEmailAsync(string email);

    /// <summary>
    /// Pseudonymizes data (reversible with key).
    /// </summary>
    Task<PseudonymizationResult> PseudonymizeAsync(string email, string key);

    /// <summary>
    /// Reverses pseudonymization with the correct key.
    /// </summary>
    Task<DepseudonymizationResult> DepseudonymizeAsync(string pseudonymizedId, string key);
}

/// <summary>
/// GDPR compliance status.
/// </summary>
public enum GdprComplianceStatus
{
    /// <summary>
    /// System is fully GDPR compliant.
    /// </summary>
    Compliant,

    /// <summary>
    /// System has warnings but is generally compliant.
    /// </summary>
    Warning,

    /// <summary>
    /// System has compliance issues that need attention.
    /// </summary>
    NonCompliant
}

/// <summary>
/// Compliance issue severity.
/// </summary>
public enum ComplianceIssueSeverity
{
    /// <summary>
    /// Informational issue (best practice recommendation).
    /// </summary>
    Info,

    /// <summary>
    /// Warning (should be addressed).
    /// </summary>
    Warning,

    /// <summary>
    /// Error (must be addressed for compliance).
    /// </summary>
    Error,

    /// <summary>
    /// Critical (immediate action required).
    /// </summary>
    Critical
}

/// <summary>
/// GDPR compliance report.
/// </summary>
public record GdprComplianceReport
{
    /// <summary>
    /// Report generation timestamp.
    /// </summary>
    public DateTimeOffset GeneratedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Overall compliance status.
    /// </summary>
    public GdprComplianceStatus Status { get; init; }

    /// <summary>
    /// Total number of consents configured.
    /// </summary>
    public int TotalConsents { get; init; }

    /// <summary>
    /// Number of active consent agreements.
    /// </summary>
    public int ActiveConsentAgreements { get; init; }

    /// <summary>
    /// Number of pending data subject requests.
    /// </summary>
    public int PendingDataRequests { get; init; }

    /// <summary>
    /// List of compliance issues.
    /// </summary>
    public IList<ComplianceIssue> Issues { get; init; } = [];

    /// <summary>
    /// Data processing activities summary.
    /// </summary>
    public DataProcessingSummary DataProcessing { get; init; } = new();

    /// <summary>
    /// Data retention status.
    /// </summary>
    public DataRetentionStatus RetentionStatus { get; init; } = new();
}

/// <summary>
/// Compliance issue details.
/// </summary>
public record ComplianceIssue
{
    /// <summary>
    /// Issue code for reference.
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Issue severity.
    /// </summary>
    public ComplianceIssueSeverity Severity { get; init; }

    /// <summary>
    /// Issue description.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Recommended remediation.
    /// </summary>
    public string? Remediation { get; init; }

    /// <summary>
    /// Related GDPR article.
    /// </summary>
    public string? GdprArticle { get; init; }

    /// <summary>
    /// When the issue was detected.
    /// </summary>
    public DateTimeOffset DetectedAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Data processing activity for audit log.
/// </summary>
public record DataProcessingActivity
{
    /// <summary>
    /// Activity ID.
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Type of processing activity.
    /// </summary>
    public required string ActivityType { get; init; }

    /// <summary>
    /// Description of the activity.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Purpose of the data processing.
    /// </summary>
    public string? Purpose { get; init; }

    /// <summary>
    /// Legal basis for processing (e.g., consent, legitimate interest).
    /// </summary>
    public string? LegalBasis { get; init; }

    /// <summary>
    /// Categories of data processed.
    /// </summary>
    public IList<string> DataCategories { get; init; } = [];

    /// <summary>
    /// Number of data subjects affected.
    /// </summary>
    public int? DataSubjectsCount { get; init; }

    /// <summary>
    /// User or system that performed the activity.
    /// </summary>
    public string? PerformedBy { get; init; }

    /// <summary>
    /// Timestamp of the activity.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Summary of data processing activities.
/// </summary>
public record DataProcessingSummary
{
    /// <summary>
    /// Total activities in the period.
    /// </summary>
    public int TotalActivities { get; init; }

    /// <summary>
    /// Activities by type.
    /// </summary>
    public Dictionary<string, int> ByType { get; init; } = [];

    /// <summary>
    /// Period covered.
    /// </summary>
    public DateTimeOffset? FromDate { get; init; }

    /// <summary>
    /// Period end date.
    /// </summary>
    public DateTimeOffset? ToDate { get; init; }
}

/// <summary>
/// Data retention policy configuration.
/// </summary>
public record DataRetentionPolicy
{
    /// <summary>
    /// Policy name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Description of the policy.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Data type this policy applies to.
    /// </summary>
    public required string DataType { get; init; }

    /// <summary>
    /// Retention period in days.
    /// </summary>
    public int RetentionDays { get; init; }

    /// <summary>
    /// Action to take when retention period expires.
    /// </summary>
    public DataRetentionAction Action { get; init; }

    /// <summary>
    /// Whether the policy is active.
    /// </summary>
    public bool IsActive { get; init; }

    /// <summary>
    /// Last execution time.
    /// </summary>
    public DateTimeOffset? LastExecuted { get; init; }
}

/// <summary>
/// Action to take when retention period expires.
/// </summary>
public enum DataRetentionAction
{
    /// <summary>
    /// Delete the data.
    /// </summary>
    Delete,

    /// <summary>
    /// Anonymize the data.
    /// </summary>
    Anonymize,

    /// <summary>
    /// Archive the data.
    /// </summary>
    Archive,

    /// <summary>
    /// Flag for review.
    /// </summary>
    FlagForReview
}

/// <summary>
/// Result of data retention execution.
/// </summary>
public record DataRetentionResult
{
    /// <summary>
    /// Whether execution was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Number of records processed.
    /// </summary>
    public int RecordsProcessed { get; init; }

    /// <summary>
    /// Number of records deleted.
    /// </summary>
    public int RecordsDeleted { get; init; }

    /// <summary>
    /// Number of records anonymized.
    /// </summary>
    public int RecordsAnonymized { get; init; }

    /// <summary>
    /// Number of records archived.
    /// </summary>
    public int RecordsArchived { get; init; }

    /// <summary>
    /// Execution timestamp.
    /// </summary>
    public DateTimeOffset ExecutedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Any errors encountered.
    /// </summary>
    public IList<string> Errors { get; init; } = [];
}

/// <summary>
/// Preview of data retention effects.
/// </summary>
public record DataRetentionPreview
{
    /// <summary>
    /// Total records that would be affected.
    /// </summary>
    public int TotalRecords { get; init; }

    /// <summary>
    /// Records by data type.
    /// </summary>
    public Dictionary<string, int> ByDataType { get; init; } = [];

    /// <summary>
    /// Records by retention action.
    /// </summary>
    public Dictionary<DataRetentionAction, int> ByAction { get; init; } = [];
}

/// <summary>
/// Status of data retention.
/// </summary>
public record DataRetentionStatus
{
    /// <summary>
    /// Number of active policies.
    /// </summary>
    public int ActivePolicies { get; init; }

    /// <summary>
    /// Last retention run.
    /// </summary>
    public DateTimeOffset? LastRun { get; init; }

    /// <summary>
    /// Next scheduled run.
    /// </summary>
    public DateTimeOffset? NextRun { get; init; }

    /// <summary>
    /// Records pending cleanup.
    /// </summary>
    public int PendingRecords { get; init; }
}

/// <summary>
/// Result of anonymization operation.
/// </summary>
public record AnonymizationResult(
    bool Success,
    int FieldsAnonymized,
    string? Message
);

/// <summary>
/// Result of pseudonymization operation.
/// </summary>
public record PseudonymizationResult(
    bool Success,
    string PseudonymizedId,
    string? Message
);

/// <summary>
/// Result of depseudonymization operation.
/// </summary>
public record DepseudonymizationResult(
    bool Success,
    string? OriginalEmail,
    string? Message
);
