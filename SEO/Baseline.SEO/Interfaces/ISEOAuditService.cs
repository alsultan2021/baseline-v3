namespace Baseline.SEO;

/// <summary>
/// Service for comprehensive SEO auditing.
/// Analyzes pages for SEO issues and provides actionable recommendations.
/// </summary>
/// <remarks>
/// <b>No Core overlap.</b> Core's <c>ISeoMetadataService</c> extracts metadata from
/// content items; this interface <i>audits</i> pages for SEO quality (score, findings,
/// Core Web Vitals, structured data validation). Complementary — use Core for metadata
/// extraction and this service for quality assessment.
/// </remarks>
public interface ISEOAuditService
{
    /// <summary>
    /// Performs a comprehensive SEO audit on a single page.
    /// </summary>
    /// <param name="url">The URL to audit.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Detailed audit results.</returns>
    Task<SEOAuditResult> AuditPageAsync(
        string url,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs SEO audit on HTML content directly.
    /// </summary>
    /// <param name="html">The HTML content.</param>
    /// <param name="url">The URL for context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Audit results.</returns>
    Task<SEOAuditResult> AuditHtmlAsync(
        string html,
        string url,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Audits multiple pages (entire site or section).
    /// </summary>
    /// <param name="urls">URLs to audit. If null, crawls from homepage.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Site-wide audit results.</returns>
    Task<SiteAuditResult> AuditSiteAsync(
        IEnumerable<string>? urls = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a detailed SEO report.
    /// </summary>
    /// <param name="since">Only include audits since this date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>SEO report.</returns>
    Task<SEOReport> GenerateReportAsync(
        DateTime? since = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets historical SEO scores for trend analysis.
    /// </summary>
    /// <param name="url">The URL to get history for.</param>
    /// <param name="count">Number of historical records.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Historical audit scores.</returns>
    Task<IEnumerable<SEOScoreHistory>> GetScoreHistoryAsync(
        string url,
        int count = 30,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates structured data on a page.
    /// </summary>
    /// <param name="url">The URL to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Structured data validation results.</returns>
    Task<StructuredDataValidation> ValidateStructuredDataAsync(
        string url,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks Core Web Vitals for a page.
    /// </summary>
    /// <param name="url">The URL to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Core Web Vitals metrics.</returns>
    Task<CoreWebVitals> CheckCoreWebVitalsAsync(
        string url,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a single page SEO audit.
/// </summary>
public record SEOAuditResult
{
    /// <summary>
    /// The audited URL.
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// Overall SEO score (0-100).
    /// </summary>
    public int Score { get; init; }

    /// <summary>
    /// Score by category.
    /// </summary>
    public SEOScoresByCategory CategoryScores { get; init; } = new();

    /// <summary>
    /// All findings from the audit.
    /// </summary>
    public IReadOnlyList<SEOAuditFinding> Findings { get; init; } = [];

    /// <summary>
    /// Summary of issues by severity.
    /// </summary>
    public IssueSummary Summary { get; init; } = new();

    /// <summary>
    /// Page metadata extracted during audit.
    /// </summary>
    public PageMetadata Metadata { get; init; } = new();

    /// <summary>
    /// Timestamp of the audit.
    /// </summary>
    public DateTime AuditedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Time taken for audit in milliseconds.
    /// </summary>
    public long DurationMs { get; init; }
}

/// <summary>
/// SEO scores broken down by category.
/// </summary>
public record SEOScoresByCategory
{
    /// <summary>
    /// Technical SEO score.
    /// </summary>
    public int Technical { get; init; }

    /// <summary>
    /// Content quality score.
    /// </summary>
    public int Content { get; init; }

    /// <summary>
    /// On-page SEO score.
    /// </summary>
    public int OnPage { get; init; }

    /// <summary>
    /// User experience score.
    /// </summary>
    public int UserExperience { get; init; }

    /// <summary>
    /// Mobile-friendliness score.
    /// </summary>
    public int Mobile { get; init; }

    /// <summary>
    /// Structured data score.
    /// </summary>
    public int StructuredData { get; init; }
}

/// <summary>
/// Summary of issues found.
/// </summary>
public record IssueSummary
{
    public int Critical { get; init; }
    public int Errors { get; init; }
    public int Warnings { get; init; }
    public int Info { get; init; }
    public int Passed { get; init; }
}

/// <summary>
/// Metadata extracted from page.
/// </summary>
public record PageMetadata
{
    public string? Title { get; init; }
    public int TitleLength { get; init; }
    public string? MetaDescription { get; init; }
    public int MetaDescriptionLength { get; init; }
    public string? CanonicalUrl { get; init; }
    public string? Robots { get; init; }
    public IReadOnlyList<string> H1Tags { get; init; } = [];
    public IReadOnlyList<HeadingStructure> Headings { get; init; } = [];
    public int WordCount { get; init; }
    public int ImageCount { get; init; }
    public int ImagesWithoutAlt { get; init; }
    public int InternalLinks { get; init; }
    public int ExternalLinks { get; init; }
    public IReadOnlyList<string> StructuredDataTypes { get; init; } = [];
    public bool HasOpenGraph { get; init; }
    public bool HasTwitterCards { get; init; }
}

/// <summary>
/// Heading structure information.
/// </summary>
public record HeadingStructure
{
    public required string Level { get; init; }
    public required string Text { get; init; }
}

/// <summary>
/// Result of site-wide audit.
/// </summary>
public record SiteAuditResult
{
    /// <summary>
    /// Average SEO score across all pages.
    /// </summary>
    public int AverageScore { get; init; }

    /// <summary>
    /// Total pages audited.
    /// </summary>
    public int TotalPages { get; init; }

    /// <summary>
    /// Pages with critical issues.
    /// </summary>
    public int PagesWithCriticalIssues { get; init; }

    /// <summary>
    /// Individual page results.
    /// </summary>
    public IReadOnlyList<SEOAuditResult> PageResults { get; init; } = [];

    /// <summary>
    /// Site-wide issues (affect multiple pages).
    /// </summary>
    public IReadOnlyList<SiteWideIssue> SiteWideIssues { get; init; } = [];

    /// <summary>
    /// Top issues by frequency.
    /// </summary>
    public IReadOnlyList<IssueFrequency> TopIssues { get; init; } = [];

    /// <summary>
    /// Score distribution.
    /// </summary>
    public ScoreDistribution Distribution { get; init; } = new();

    /// <summary>
    /// Audit timestamp.
    /// </summary>
    public DateTime AuditedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Total audit duration in milliseconds.
    /// </summary>
    public long TotalDurationMs { get; init; }
}

/// <summary>
/// A site-wide issue affecting multiple pages.
/// </summary>
public record SiteWideIssue
{
    public required string Category { get; init; }
    public required string Issue { get; init; }
    public int AffectedPages { get; init; }
    public SEOAuditSeverity Severity { get; init; }
    public required string Recommendation { get; init; }
}

/// <summary>
/// Frequency of a specific issue.
/// </summary>
public record IssueFrequency
{
    public required string Issue { get; init; }
    public int Count { get; init; }
    public SEOAuditSeverity Severity { get; init; }
}

/// <summary>
/// Distribution of SEO scores.
/// </summary>
public record ScoreDistribution
{
    public int Excellent { get; init; } // 90-100
    public int Good { get; init; }      // 70-89
    public int NeedsWork { get; init; } // 50-69
    public int Poor { get; init; }      // 0-49
}

/// <summary>
/// SEO report with trends and recommendations.
/// </summary>
public record SEOReport
{
    /// <summary>
    /// Report generation timestamp.
    /// </summary>
    public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Report period.
    /// </summary>
    public DateRange Period { get; init; } = new();

    /// <summary>
    /// Executive summary.
    /// </summary>
    public required string Summary { get; init; }

    /// <summary>
    /// Current average score.
    /// </summary>
    public int CurrentScore { get; init; }

    /// <summary>
    /// Score change from previous period.
    /// </summary>
    public int ScoreChange { get; init; }

    /// <summary>
    /// Top priorities.
    /// </summary>
    public IReadOnlyList<SEOPriority> Priorities { get; init; } = [];

    /// <summary>
    /// Improvements made.
    /// </summary>
    public IReadOnlyList<SEOImprovement> Improvements { get; init; } = [];

    /// <summary>
    /// New issues detected.
    /// </summary>
    public IReadOnlyList<SEOIssue> NewIssues { get; init; } = [];

    /// <summary>
    /// Trend data for charts.
    /// </summary>
    public IReadOnlyList<TrendDataPoint> Trends { get; init; } = [];
}

/// <summary>
/// Date range for report.
/// </summary>
public record DateRange
{
    public DateTime Start { get; init; }
    public DateTime End { get; init; }
}

/// <summary>
/// A priority action item.
/// </summary>
public record SEOPriority
{
    public required string Title { get; init; }
    public required string Description { get; init; }
    public int ImpactScore { get; init; }
    public int EffortLevel { get; init; }
    public IReadOnlyList<string> AffectedUrls { get; init; } = [];
}

/// <summary>
/// An improvement made.
/// </summary>
public record SEOImprovement
{
    public required string Description { get; init; }
    public DateTime Date { get; init; }
    public int ScoreImpact { get; init; }
}

/// <summary>
/// A new issue detected.
/// </summary>
public record SEOIssue
{
    public required string Description { get; init; }
    public DateTime FirstDetected { get; init; }
    public SEOAuditSeverity Severity { get; init; }
    public int AffectedPages { get; init; }
}

/// <summary>
/// A data point for trend charts.
/// </summary>
public record TrendDataPoint
{
    public DateTime Date { get; init; }
    public int Score { get; init; }
    public int PageCount { get; init; }
    public int IssueCount { get; init; }
}

/// <summary>
/// Historical SEO score.
/// </summary>
public record SEOScoreHistory
{
    public required string Url { get; init; }
    public DateTime Date { get; init; }
    public int Score { get; init; }
    public int CriticalIssues { get; init; }
}

/// <summary>
/// Structured data validation results.
/// </summary>
public record StructuredDataValidation
{
    public required string Url { get; init; }
    public bool IsValid { get; init; }
    public IReadOnlyList<StructuredDataItem> ValidItems { get; init; } = [];
    public IReadOnlyList<StructuredDataError> Errors { get; init; } = [];
    public IReadOnlyList<StructuredDataWarning> Warnings { get; init; } = [];
}

/// <summary>
/// A structured data validation error.
/// </summary>
public record StructuredDataError
{
    public required string Type { get; init; }
    public required string Message { get; init; }
    public string? Path { get; init; }
}

/// <summary>
/// A structured data validation warning.
/// </summary>
public record StructuredDataWarning
{
    public required string Type { get; init; }
    public required string Message { get; init; }
    public string? Recommendation { get; init; }
}

/// <summary>
/// Core Web Vitals metrics.
/// </summary>
public record CoreWebVitals
{
    /// <summary>
    /// Largest Contentful Paint in milliseconds.
    /// Good: ≤2500ms, Needs Improvement: ≤4000ms, Poor: >4000ms
    /// </summary>
    public double? LCP { get; init; }

    /// <summary>
    /// First Input Delay in milliseconds.
    /// Good: ≤100ms, Needs Improvement: ≤300ms, Poor: >300ms
    /// </summary>
    [Obsolete("FID was deprecated by Google in March 2024. Use INP (Interaction to Next Paint) instead.")]
    public double? FID { get; init; }

    /// <summary>
    /// Cumulative Layout Shift.
    /// Good: ≤0.1, Needs Improvement: ≤0.25, Poor: >0.25
    /// </summary>
    public double? CLS { get; init; }

    /// <summary>
    /// Interaction to Next Paint in milliseconds.
    /// Good: ≤200ms, Needs Improvement: ≤500ms, Poor: >500ms
    /// </summary>
    public double? INP { get; init; }

    /// <summary>
    /// Time to First Byte in milliseconds.
    /// </summary>
    public double? TTFB { get; init; }

    /// <summary>
    /// First Contentful Paint in milliseconds.
    /// </summary>
    public double? FCP { get; init; }

    /// <summary>
    /// Overall performance score (0-100).
    /// </summary>
    public int PerformanceScore { get; init; }

    /// <summary>
    /// Assessment for each metric.
    /// </summary>
    public CoreWebVitalsAssessment Assessment { get; init; } = new();
}

/// <summary>
/// Assessment of Core Web Vitals.
/// </summary>
public record CoreWebVitalsAssessment
{
    public VitalStatus LCP { get; init; }

    [Obsolete("FID was deprecated by Google in March 2024. Use INP instead.")]
    public VitalStatus FID { get; init; }
    public VitalStatus CLS { get; init; }
    public VitalStatus INP { get; init; }
}

/// <summary>
/// Status of a Core Web Vital.
/// </summary>
public enum VitalStatus
{
    Good,
    NeedsImprovement,
    Poor,
    Unknown
}
