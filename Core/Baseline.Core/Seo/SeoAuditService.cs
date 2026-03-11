using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baseline.Core.Seo;

/// <summary>
/// Service for automated SEO auditing and scoring.
/// Analyzes pages for SEO best practices and provides recommendations.
/// </summary>
public interface ISeoAuditService
{
    /// <summary>
    /// Audits a single page for SEO issues.
    /// </summary>
    /// <param name="pageData">The page data to audit.</param>
    /// <returns>SEO audit result.</returns>
    Task<SeoAuditResult> AuditPageAsync(PageAuditData pageData);

    /// <summary>
    /// Audits multiple pages and aggregates results.
    /// </summary>
    /// <param name="pages">The pages to audit.</param>
    /// <returns>Site-wide audit result.</returns>
    Task<SiteAuditResult> AuditSiteAsync(IEnumerable<PageAuditData> pages);

    /// <summary>
    /// Generates a detailed SEO report.
    /// </summary>
    /// <param name="auditResult">The audit result to generate report from.</param>
    /// <returns>Formatted SEO report.</returns>
    Task<SeoReport> GenerateReportAsync(SiteAuditResult auditResult);

    /// <summary>
    /// Gets SEO recommendations for a page.
    /// </summary>
    /// <param name="pageData">The page data to analyze.</param>
    /// <returns>List of recommendations.</returns>
    Task<IEnumerable<SeoRecommendation>> GetRecommendationsAsync(PageAuditData pageData);

    /// <summary>
    /// Validates structured data on a page.
    /// </summary>
    /// <param name="jsonLd">The JSON-LD content to validate.</param>
    /// <returns>Validation result.</returns>
    Task<StructuredDataValidation> ValidateStructuredDataAsync(string jsonLd);
}

/// <summary>
/// Data required to audit a page.
/// </summary>
public sealed record PageAuditData
{
    /// <summary>Page URL.</summary>
    public string Url { get; init; } = string.Empty;

    /// <summary>Page title.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Meta description.</summary>
    public string MetaDescription { get; init; } = string.Empty;

    /// <summary>H1 heading.</summary>
    public string H1 { get; init; } = string.Empty;

    /// <summary>All headings on the page.</summary>
    public IReadOnlyList<HeadingInfo> Headings { get; init; } = [];

    /// <summary>Main content text.</summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>Images on the page.</summary>
    public IReadOnlyList<ImageInfo> Images { get; init; } = [];

    /// <summary>Links on the page.</summary>
    public IReadOnlyList<LinkInfo> Links { get; init; } = [];

    /// <summary>Canonical URL.</summary>
    public string? CanonicalUrl { get; init; }

    /// <summary>Open Graph data.</summary>
    public SeoAuditOgData? OpenGraph { get; init; }

    /// <summary>JSON-LD structured data.</summary>
    public string? JsonLd { get; init; }

    /// <summary>Page load time in milliseconds.</summary>
    public int? LoadTimeMs { get; init; }

    /// <summary>Whether the page is mobile-friendly.</summary>
    public bool? IsMobileFriendly { get; init; }

    /// <summary>Content language.</summary>
    public string? Language { get; init; }

    /// <summary>Last modified date.</summary>
    public DateTimeOffset? LastModified { get; init; }
}

/// <summary>
/// Heading information.
/// </summary>
public sealed record HeadingInfo
{
    /// <summary>Heading level (1-6).</summary>
    public int Level { get; init; }

    /// <summary>Heading text.</summary>
    public string Text { get; init; } = string.Empty;
}

/// <summary>
/// Image information for SEO audit.
/// </summary>
public sealed record ImageInfo
{
    /// <summary>Image source URL.</summary>
    public string Src { get; init; } = string.Empty;

    /// <summary>Alt text.</summary>
    public string? Alt { get; init; }

    /// <summary>Image width.</summary>
    public int? Width { get; init; }

    /// <summary>Image height.</summary>
    public int? Height { get; init; }

    /// <summary>Whether image uses lazy loading.</summary>
    public bool IsLazyLoaded { get; init; }
}

/// <summary>
/// Link information for SEO audit.
/// </summary>
public sealed record LinkInfo
{
    /// <summary>Link URL.</summary>
    public string Href { get; init; } = string.Empty;

    /// <summary>Link text.</summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>Whether this is an external link.</summary>
    public bool IsExternal { get; init; }

    /// <summary>Whether the link has rel="nofollow".</summary>
    public bool IsNoFollow { get; init; }
}

/// <summary>
/// Open Graph data for SEO audit analysis.
/// </summary>
public sealed record SeoAuditOgData
{
    /// <summary>OG title.</summary>
    public string? Title { get; init; }

    /// <summary>OG description.</summary>
    public string? Description { get; init; }

    /// <summary>OG image.</summary>
    public string? Image { get; init; }

    /// <summary>OG type.</summary>
    public string? Type { get; init; }
}

/// <summary>
/// SEO audit result for a single page.
/// </summary>
public sealed record SeoAuditResult
{
    /// <summary>Page URL.</summary>
    public string Url { get; init; } = string.Empty;

    /// <summary>Overall SEO score (0-100).</summary>
    public int Score { get; init; }

    /// <summary>Score category (Poor, Fair, Good, Excellent).</summary>
    public string ScoreCategory => Score switch
    {
        >= 90 => "Excellent",
        >= 70 => "Good",
        >= 50 => "Fair",
        _ => "Poor"
    };

    /// <summary>Individual check results.</summary>
    public IReadOnlyList<SeoCheck> Checks { get; init; } = [];

    /// <summary>Critical issues found.</summary>
    public IReadOnlyList<SeoIssue> CriticalIssues { get; init; } = [];

    /// <summary>Warnings found.</summary>
    public IReadOnlyList<SeoIssue> Warnings { get; init; } = [];

    /// <summary>Passed checks count.</summary>
    public int PassedChecks => Checks.Count(c => c.Passed);

    /// <summary>Failed checks count.</summary>
    public int FailedChecks => Checks.Count(c => !c.Passed);

    /// <summary>Audit timestamp.</summary>
    public DateTimeOffset AuditedAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Individual SEO check result.
/// </summary>
public sealed record SeoCheck
{
    /// <summary>Check name.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Check category.</summary>
    public string Category { get; init; } = string.Empty;

    /// <summary>Whether the check passed.</summary>
    public bool Passed { get; init; }

    /// <summary>Score contribution (0-100).</summary>
    public int Score { get; init; }

    /// <summary>Maximum possible score.</summary>
    public int MaxScore { get; init; }

    /// <summary>Check description.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>Current value found.</summary>
    public string? CurrentValue { get; init; }

    /// <summary>Recommended value or action.</summary>
    public string? Recommendation { get; init; }
}

/// <summary>
/// SEO issue found during audit.
/// </summary>
public sealed record SeoIssue
{
    /// <summary>Issue type.</summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>Issue severity (Critical, Warning, Info).</summary>
    public IssueSeverity Severity { get; init; }

    /// <summary>Issue description.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>How to fix the issue.</summary>
    public string Recommendation { get; init; } = string.Empty;

    /// <summary>Impact on SEO (High, Medium, Low).</summary>
    public string Impact { get; init; } = string.Empty;
}

/// <summary>
/// Issue severity levels.
/// </summary>
public enum IssueSeverity
{
    Info,
    Warning,
    Critical
}

/// <summary>
/// Site-wide audit result.
/// </summary>
public sealed record SiteAuditResult
{
    /// <summary>Average SEO score across all pages.</summary>
    public int AverageScore { get; init; }

    /// <summary>Total pages audited.</summary>
    public int TotalPages { get; init; }

    /// <summary>Pages with excellent score (90+).</summary>
    public int ExcellentPages { get; init; }

    /// <summary>Pages with good score (70-89).</summary>
    public int GoodPages { get; init; }

    /// <summary>Pages with fair score (50-69).</summary>
    public int FairPages { get; init; }

    /// <summary>Pages with poor score (0-49).</summary>
    public int PoorPages { get; init; }

    /// <summary>Most common issues across the site.</summary>
    public IReadOnlyList<CommonIssue> CommonIssues { get; init; } = [];

    /// <summary>Individual page results.</summary>
    public IReadOnlyList<SeoAuditResult> PageResults { get; init; } = [];

    /// <summary>Audit timestamp.</summary>
    public DateTimeOffset AuditedAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Common issue found across multiple pages.
/// </summary>
public sealed record CommonIssue
{
    /// <summary>Issue type.</summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>Number of pages affected.</summary>
    public int AffectedPages { get; init; }

    /// <summary>Percentage of pages affected.</summary>
    public double PercentageAffected { get; init; }

    /// <summary>Issue severity.</summary>
    public IssueSeverity Severity { get; init; }

    /// <summary>Recommendation to fix.</summary>
    public string Recommendation { get; init; } = string.Empty;
}

/// <summary>
/// SEO recommendation.
/// </summary>
public sealed record SeoRecommendation
{
    /// <summary>Recommendation category.</summary>
    public string Category { get; init; } = string.Empty;

    /// <summary>Priority (1 = highest).</summary>
    public int Priority { get; init; }

    /// <summary>Recommendation title.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Detailed description.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>Expected impact on SEO score.</summary>
    public int ExpectedImpact { get; init; }

    /// <summary>Effort required (Low, Medium, High).</summary>
    public string Effort { get; init; } = string.Empty;
}

/// <summary>
/// Structured data validation result.
/// </summary>
public sealed record StructuredDataValidation
{
    /// <summary>Whether the structured data is valid.</summary>
    public bool IsValid { get; init; }

    /// <summary>Detected schema types.</summary>
    public IReadOnlyList<string> DetectedTypes { get; init; } = [];

    /// <summary>Validation errors.</summary>
    public IReadOnlyList<string> Errors { get; init; } = [];

    /// <summary>Validation warnings.</summary>
    public IReadOnlyList<string> Warnings { get; init; } = [];

    /// <summary>Recommended improvements.</summary>
    public IReadOnlyList<string> Recommendations { get; init; } = [];
}

/// <summary>
/// SEO report.
/// </summary>
public sealed record SeoReport
{
    /// <summary>Report title.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Report generated at.</summary>
    public DateTimeOffset GeneratedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Executive summary.</summary>
    public string Summary { get; init; } = string.Empty;

    /// <summary>Site audit results.</summary>
    public SiteAuditResult AuditResult { get; init; } = new();

    /// <summary>Top recommendations.</summary>
    public IReadOnlyList<SeoRecommendation> TopRecommendations { get; init; } = [];

    /// <summary>Report sections.</summary>
    public IReadOnlyList<ReportSection> Sections { get; init; } = [];
}

/// <summary>
/// A section of the SEO report.
/// </summary>
public sealed record ReportSection
{
    /// <summary>Section title.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Section content (markdown).</summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>Section score if applicable.</summary>
    public int? Score { get; init; }
}

/// <summary>
/// SEO audit configuration options.
/// </summary>
public sealed class SeoAuditOptions
{
    /// <summary>Minimum title length.</summary>
    public int MinTitleLength { get; set; } = 30;

    /// <summary>Maximum title length.</summary>
    public int MaxTitleLength { get; set; } = 60;

    /// <summary>Minimum meta description length.</summary>
    public int MinMetaDescriptionLength { get; set; } = 120;

    /// <summary>Maximum meta description length.</summary>
    public int MaxMetaDescriptionLength { get; set; } = 160;

    /// <summary>Minimum content word count.</summary>
    public int MinContentWordCount { get; set; } = 300;

    /// <summary>Maximum page load time in milliseconds.</summary>
    public int MaxLoadTimeMs { get; set; } = 3000;

    /// <summary>Enable GEO checks.</summary>
    public bool EnableGeoChecks { get; set; } = true;
}

/// <summary>
/// Default implementation of SEO audit service.
/// </summary>
internal sealed class SeoAuditService(
    IOptions<SeoAuditOptions> options,
    ILogger<SeoAuditService> logger) : ISeoAuditService
{
    private readonly SeoAuditOptions _options = options.Value;

    public Task<SeoAuditResult> AuditPageAsync(PageAuditData pageData)
    {
        var checks = new List<SeoCheck>();
        var criticalIssues = new List<SeoIssue>();
        var warnings = new List<SeoIssue>();

        // Title checks
        var titleCheck = CheckTitle(pageData.Title);
        checks.Add(titleCheck);
        if (!titleCheck.Passed)
        {
            var severity = string.IsNullOrEmpty(pageData.Title) ? IssueSeverity.Critical : IssueSeverity.Warning;
            (severity == IssueSeverity.Critical ? criticalIssues : warnings).Add(new SeoIssue
            {
                Type = "Title",
                Severity = severity,
                Description = titleCheck.Description,
                Recommendation = titleCheck.Recommendation ?? "Add a descriptive title",
                Impact = "High"
            });
        }

        // Meta description checks
        var metaCheck = CheckMetaDescription(pageData.MetaDescription);
        checks.Add(metaCheck);
        if (!metaCheck.Passed)
        {
            warnings.Add(new SeoIssue
            {
                Type = "Meta Description",
                Severity = IssueSeverity.Warning,
                Description = metaCheck.Description,
                Recommendation = metaCheck.Recommendation ?? "Add a compelling meta description",
                Impact = "Medium"
            });
        }

        // H1 checks
        var h1Check = CheckH1(pageData.H1, pageData.Headings);
        checks.Add(h1Check);
        if (!h1Check.Passed)
        {
            criticalIssues.Add(new SeoIssue
            {
                Type = "H1 Heading",
                Severity = IssueSeverity.Critical,
                Description = h1Check.Description,
                Recommendation = h1Check.Recommendation ?? "Add a single H1 heading",
                Impact = "High"
            });
        }

        // Content checks
        var contentCheck = CheckContent(pageData.Content);
        checks.Add(contentCheck);
        if (!contentCheck.Passed)
        {
            warnings.Add(new SeoIssue
            {
                Type = "Content",
                Severity = IssueSeverity.Warning,
                Description = contentCheck.Description,
                Recommendation = contentCheck.Recommendation ?? "Add more content",
                Impact = "Medium"
            });
        }

        // Image alt text checks
        var imageCheck = CheckImages(pageData.Images);
        checks.Add(imageCheck);
        if (!imageCheck.Passed)
        {
            warnings.Add(new SeoIssue
            {
                Type = "Images",
                Severity = IssueSeverity.Warning,
                Description = imageCheck.Description,
                Recommendation = imageCheck.Recommendation ?? "Add alt text to all images",
                Impact = "Medium"
            });
        }

        // Canonical URL check
        var canonicalCheck = CheckCanonical(pageData.CanonicalUrl, pageData.Url);
        checks.Add(canonicalCheck);

        // Open Graph check
        var ogCheck = CheckOpenGraph(pageData.OpenGraph);
        checks.Add(ogCheck);

        // Structured data check
        var structuredDataCheck = CheckStructuredData(pageData.JsonLd);
        checks.Add(structuredDataCheck);

        // Calculate overall score
        var totalMaxScore = checks.Sum(c => c.MaxScore);
        var totalScore = checks.Sum(c => c.Score);
        var overallScore = totalMaxScore > 0 ? (int)((double)totalScore / totalMaxScore * 100) : 0;

        logger.LogDebug("SEO audit completed for {Url}. Score: {Score}", pageData.Url, overallScore);

        return Task.FromResult(new SeoAuditResult
        {
            Url = pageData.Url,
            Score = overallScore,
            Checks = checks,
            CriticalIssues = criticalIssues,
            Warnings = warnings
        });
    }

    public async Task<SiteAuditResult> AuditSiteAsync(IEnumerable<PageAuditData> pages)
    {
        var pageList = pages.ToList();
        var results = new List<SeoAuditResult>();

        foreach (var page in pageList)
        {
            var result = await AuditPageAsync(page);
            results.Add(result);
        }

        var issueGroups = results
            .SelectMany(r => r.CriticalIssues.Concat(r.Warnings))
            .GroupBy(i => i.Type)
            .Select(g => new CommonIssue
            {
                Type = g.Key,
                AffectedPages = g.Count(),
                PercentageAffected = pageList.Count > 0 ? (double)g.Count() / pageList.Count * 100 : 0,
                Severity = g.Any(i => i.Severity == IssueSeverity.Critical) ? IssueSeverity.Critical : IssueSeverity.Warning,
                Recommendation = g.First().Recommendation
            })
            .OrderByDescending(i => i.AffectedPages)
            .ToList();

        return new SiteAuditResult
        {
            AverageScore = results.Count > 0 ? (int)results.Average(r => r.Score) : 0,
            TotalPages = results.Count,
            ExcellentPages = results.Count(r => r.Score >= 90),
            GoodPages = results.Count(r => r.Score is >= 70 and < 90),
            FairPages = results.Count(r => r.Score is >= 50 and < 70),
            PoorPages = results.Count(r => r.Score < 50),
            CommonIssues = issueGroups,
            PageResults = results
        };
    }

    public Task<SeoReport> GenerateReportAsync(SiteAuditResult auditResult)
    {
        var recommendations = auditResult.CommonIssues
            .OrderByDescending(i => i.Severity)
            .ThenByDescending(i => i.AffectedPages)
            .Take(5)
            .Select((issue, index) => new SeoRecommendation
            {
                Category = issue.Type,
                Priority = index + 1,
                Title = $"Fix {issue.Type} issues",
                Description = issue.Recommendation,
                ExpectedImpact = issue.Severity == IssueSeverity.Critical ? 15 : 10,
                Effort = issue.AffectedPages > auditResult.TotalPages / 2 ? "High" : "Medium"
            })
            .ToList();

        var summary = $"SEO audit completed for {auditResult.TotalPages} pages. " +
            $"Average score: {auditResult.AverageScore}/100. " +
            $"{auditResult.ExcellentPages} excellent, {auditResult.GoodPages} good, " +
            $"{auditResult.FairPages} fair, {auditResult.PoorPages} poor.";

        var sections = new List<ReportSection>
        {
            new()
            {
                Title = "Overview",
                Content = $"## Site Health\n\n- **Average Score**: {auditResult.AverageScore}/100\n- **Total Pages**: {auditResult.TotalPages}\n- **Excellent (90+)**: {auditResult.ExcellentPages}\n- **Good (70-89)**: {auditResult.GoodPages}\n- **Fair (50-69)**: {auditResult.FairPages}\n- **Poor (0-49)**: {auditResult.PoorPages}",
                Score = auditResult.AverageScore
            },
            new()
            {
                Title = "Common Issues",
                Content = string.Join("\n", auditResult.CommonIssues.Select(i =>
                    $"- **{i.Type}**: {i.AffectedPages} pages ({i.PercentageAffected:F1}%) - {i.Recommendation}"))
            }
        };

        return Task.FromResult(new SeoReport
        {
            Title = "SEO Audit Report",
            Summary = summary,
            AuditResult = auditResult,
            TopRecommendations = recommendations,
            Sections = sections
        });
    }

    public async Task<IEnumerable<SeoRecommendation>> GetRecommendationsAsync(PageAuditData pageData)
    {
        var result = await AuditPageAsync(pageData);
        var recommendations = new List<SeoRecommendation>();
        var priority = 0;

        foreach (var issue in result.CriticalIssues)
        {
            priority++;
            recommendations.Add(new SeoRecommendation
            {
                Category = issue.Type,
                Priority = priority,
                Title = $"Fix: {issue.Type}",
                Description = issue.Recommendation,
                ExpectedImpact = 15,
                Effort = "Low"
            });
        }

        foreach (var issue in result.Warnings)
        {
            priority++;
            recommendations.Add(new SeoRecommendation
            {
                Category = issue.Type,
                Priority = priority,
                Title = $"Improve: {issue.Type}",
                Description = issue.Recommendation,
                ExpectedImpact = 10,
                Effort = "Low"
            });
        }

        return recommendations.OrderBy(r => r.Priority);
    }

    public Task<StructuredDataValidation> ValidateStructuredDataAsync(string jsonLd)
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        var recommendations = new List<string>();
        var detectedTypes = new List<string>();

        if (string.IsNullOrWhiteSpace(jsonLd))
        {
            return Task.FromResult(new StructuredDataValidation
            {
                IsValid = false,
                Errors = ["No structured data found"],
                Recommendations = ["Add JSON-LD structured data for better search visibility"]
            });
        }

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(jsonLd);
            var root = doc.RootElement;

            // Check for @context
            if (!root.TryGetProperty("@context", out _))
            {
                errors.Add("Missing @context property");
            }

            // Check for @type
            if (root.TryGetProperty("@type", out var typeElement))
            {
                detectedTypes.Add(typeElement.GetString() ?? "Unknown");
            }
            else
            {
                errors.Add("Missing @type property");
            }

            // Check for common required properties based on type
            if (detectedTypes.Contains("Article") || detectedTypes.Contains("BlogPosting"))
            {
                if (!root.TryGetProperty("headline", out _))
                    warnings.Add("Article schema should include 'headline'");
                if (!root.TryGetProperty("author", out _))
                    warnings.Add("Article schema should include 'author'");
                if (!root.TryGetProperty("datePublished", out _))
                    warnings.Add("Article schema should include 'datePublished'");
            }

            if (detectedTypes.Contains("Organization"))
            {
                if (!root.TryGetProperty("name", out _))
                    errors.Add("Organization schema requires 'name'");
                if (!root.TryGetProperty("logo", out _))
                    recommendations.Add("Consider adding 'logo' to Organization schema");
            }
        }
        catch (System.Text.Json.JsonException ex)
        {
            errors.Add($"Invalid JSON: {ex.Message}");
        }

        return Task.FromResult(new StructuredDataValidation
        {
            IsValid = errors.Count == 0,
            DetectedTypes = detectedTypes,
            Errors = errors,
            Warnings = warnings,
            Recommendations = recommendations
        });
    }

    private SeoCheck CheckTitle(string title)
    {
        var length = title?.Length ?? 0;
        var passed = length >= _options.MinTitleLength && length <= _options.MaxTitleLength;
        var score = passed ? 15 : (length > 0 ? 5 : 0);

        return new SeoCheck
        {
            Name = "Title Tag",
            Category = "On-Page",
            Passed = passed,
            Score = score,
            MaxScore = 15,
            Description = length == 0
                ? "Missing title tag"
                : length < _options.MinTitleLength
                    ? $"Title too short ({length} chars, min {_options.MinTitleLength})"
                    : length > _options.MaxTitleLength
                        ? $"Title too long ({length} chars, max {_options.MaxTitleLength})"
                        : "Title length is optimal",
            CurrentValue = title,
            Recommendation = passed ? null : $"Title should be {_options.MinTitleLength}-{_options.MaxTitleLength} characters"
        };
    }

    private SeoCheck CheckMetaDescription(string description)
    {
        var length = description?.Length ?? 0;
        var passed = length >= _options.MinMetaDescriptionLength && length <= _options.MaxMetaDescriptionLength;
        var score = passed ? 10 : (length > 0 ? 3 : 0);

        return new SeoCheck
        {
            Name = "Meta Description",
            Category = "On-Page",
            Passed = passed,
            Score = score,
            MaxScore = 10,
            Description = length == 0
                ? "Missing meta description"
                : length < _options.MinMetaDescriptionLength
                    ? $"Meta description too short ({length} chars)"
                    : length > _options.MaxMetaDescriptionLength
                        ? $"Meta description too long ({length} chars)"
                        : "Meta description length is optimal",
            CurrentValue = description,
            Recommendation = passed ? null : $"Description should be {_options.MinMetaDescriptionLength}-{_options.MaxMetaDescriptionLength} characters"
        };
    }

    private SeoCheck CheckH1(string h1, IReadOnlyList<HeadingInfo> headings)
    {
        var h1Count = headings?.Count(h => h.Level == 1) ?? (string.IsNullOrEmpty(h1) ? 0 : 1);
        var passed = h1Count == 1 && !string.IsNullOrWhiteSpace(h1);
        var score = passed ? 15 : (h1Count > 0 ? 5 : 0);

        return new SeoCheck
        {
            Name = "H1 Heading",
            Category = "On-Page",
            Passed = passed,
            Score = score,
            MaxScore = 15,
            Description = h1Count == 0
                ? "Missing H1 heading"
                : h1Count > 1
                    ? $"Multiple H1 headings found ({h1Count})"
                    : "Single H1 heading present",
            CurrentValue = h1,
            Recommendation = passed ? null : "Page should have exactly one H1 heading"
        };
    }

    private SeoCheck CheckContent(string content)
    {
        var wordCount = content?.Split([' ', '\t', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries).Length ?? 0;
        var passed = wordCount >= _options.MinContentWordCount;
        var score = passed ? 15 : (int)(15.0 * wordCount / _options.MinContentWordCount);

        return new SeoCheck
        {
            Name = "Content Length",
            Category = "Content",
            Passed = passed,
            Score = Math.Min(score, 15),
            MaxScore = 15,
            Description = wordCount < _options.MinContentWordCount
                ? $"Content too short ({wordCount} words, min {_options.MinContentWordCount})"
                : $"Content length is good ({wordCount} words)",
            CurrentValue = $"{wordCount} words",
            Recommendation = passed ? null : $"Add more content (minimum {_options.MinContentWordCount} words recommended)"
        };
    }

    private static SeoCheck CheckImages(IReadOnlyList<ImageInfo> images)
    {
        if (images == null || images.Count == 0)
        {
            return new SeoCheck
            {
                Name = "Image Alt Text",
                Category = "Accessibility",
                Passed = true,
                Score = 10,
                MaxScore = 10,
                Description = "No images on page"
            };
        }

        var withAlt = images.Count(i => !string.IsNullOrWhiteSpace(i.Alt));
        var percentage = (double)withAlt / images.Count * 100;
        var passed = percentage >= 90;

        return new SeoCheck
        {
            Name = "Image Alt Text",
            Category = "Accessibility",
            Passed = passed,
            Score = (int)(10 * percentage / 100),
            MaxScore = 10,
            Description = passed
                ? $"All images have alt text ({withAlt}/{images.Count})"
                : $"{images.Count - withAlt} images missing alt text",
            CurrentValue = $"{percentage:F0}% have alt text",
            Recommendation = passed ? null : "Add descriptive alt text to all images"
        };
    }

    private static SeoCheck CheckCanonical(string? canonical, string url)
    {
        var hasCanonical = !string.IsNullOrWhiteSpace(canonical);

        return new SeoCheck
        {
            Name = "Canonical URL",
            Category = "Technical",
            Passed = hasCanonical,
            Score = hasCanonical ? 5 : 0,
            MaxScore = 5,
            Description = hasCanonical ? "Canonical URL is set" : "Missing canonical URL",
            CurrentValue = canonical,
            Recommendation = hasCanonical ? null : "Add a canonical URL to prevent duplicate content issues"
        };
    }

    private static SeoCheck CheckOpenGraph(SeoAuditOgData? og)
    {
        var hasOg = og != null && !string.IsNullOrWhiteSpace(og.Title);
        var hasImage = og?.Image != null;

        return new SeoCheck
        {
            Name = "Open Graph Tags",
            Category = "Social",
            Passed = hasOg && hasImage,
            Score = (hasOg ? 5 : 0) + (hasImage ? 5 : 0),
            MaxScore = 10,
            Description = !hasOg
                ? "Missing Open Graph tags"
                : !hasImage
                    ? "Open Graph image missing"
                    : "Open Graph tags are complete",
            Recommendation = hasOg && hasImage ? null : "Add Open Graph tags for better social sharing"
        };
    }

    private static SeoCheck CheckStructuredData(string? jsonLd)
    {
        var hasStructuredData = !string.IsNullOrWhiteSpace(jsonLd);

        return new SeoCheck
        {
            Name = "Structured Data",
            Category = "Technical",
            Passed = hasStructuredData,
            Score = hasStructuredData ? 10 : 0,
            MaxScore = 10,
            Description = hasStructuredData
                ? "JSON-LD structured data found"
                : "No structured data found",
            Recommendation = hasStructuredData ? null : "Add JSON-LD structured data for rich search results"
        };
    }
}
