namespace Baseline.SEO;

/// <summary>
/// Configuration options for Baseline SEO features.
/// Includes GEO optimization, Answer Engine, and LLMs.txt settings.
/// </summary>
public class BaselineSEOOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SECTION_NAME = "BaselineSEO";

    /// <summary>
    /// Enable GEO (Generative Engine Optimization) features.
    /// GEO optimizes content for AI search engines like Perplexity, ChatGPT, etc.
    /// </summary>
    public bool EnableGEO { get; set; } = true;

    /// <summary>
    /// Enable automatic FAQ extraction from content.
    /// Extracts Q&A patterns and generates FAQPage structured data.
    /// </summary>
    public bool AutoExtractFAQs { get; set; } = true;

    /// <summary>
    /// Enable automatic HowTo extraction from instructional content.
    /// </summary>
    public bool AutoExtractHowTo { get; set; } = true;

    /// <summary>
    /// Enable SEO audit scheduling.
    /// When enabled, runs automated SEO audits on configured schedule.
    /// </summary>
    public bool EnableScheduledAudits { get; set; } = false;

    /// <summary>
    /// Minimum acceptable SEO score (0-100).
    /// Pages below this score are flagged for improvement.
    /// </summary>
    public int MinimumSEOScore { get; set; } = 70;

    /// <summary>
    /// Enable Speakable content annotations for voice search.
    /// </summary>
    public bool EnableSpeakable { get; set; } = true;

    /// <summary>
    /// LLMs.txt configuration for AI crawler optimization.
    /// </summary>
    public LLMsOptions LLMs { get; set; } = new();

    /// <summary>
    /// Answer Engine optimization settings.
    /// </summary>
    public AnswerEngineOptions AnswerEngine { get; set; } = new();

    /// <summary>
    /// SEO audit configuration.
    /// </summary>
    public SEOAuditOptions Audit { get; set; } = new();
}

/// <summary>
/// Configuration for LLMs.txt file generation.
/// LLMs.txt helps AI crawlers understand your content structure.
/// </summary>
public class LLMsOptions
{
    /// <summary>
    /// Enable LLMs.txt file generation at /llms.txt.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Override base URL for llms.txt canonical links.
    /// If null, uses WebsiteChannelDomain. Must be absolute (https://example.com).
    /// </summary>
    public string? BaseUrlOverride { get; set; }

    /// <summary>
    /// Force HTTPS scheme when building URLs. Default true.
    /// </summary>
    public bool ForceHttps { get; set; } = true;

    /// <summary>
    /// Maximum key pages to include from navigation. Default 15.
    /// </summary>
    public int MaxKeyPages { get; set; } = 15;

    /// <summary>
    /// Maximum events to include in llms.txt.
    /// </summary>
    public int MaxEvents { get; set; } = 25;

    /// <summary>
    /// Maximum FAQs per language to include.
    /// </summary>
    public int MaxFaqs { get; set; } = 15;

    /// <summary>
    /// Maximum menu items to include.
    /// </summary>
    public int MaxMenuItems { get; set; } = 50;

    /// <summary>
    /// Fallback company name if EnterpriseInformation is missing.
    /// </summary>
    public string FallbackCompanyName { get; set; } = "";

    /// <summary>
    /// Fallback category name for uncategorized items.
    /// </summary>
    public string FallbackCategoryName { get; set; } = "Other";

    /// <summary>
    /// Fallback location name when not specified.
    /// </summary>
    public string FallbackLocationName { get; set; } = "Main Location";

    /// <summary>
    /// Site timezone for "Updated" field. Default America/Toronto.
    /// </summary>
    public string SiteTimezone { get; set; } = "America/Toronto";

    /// <summary>
    /// Enable vector index reference in LLMs.txt.
    /// Points AI crawlers to your embedding API endpoint.
    /// </summary>
    public bool EnableVectorIndex { get; set; } = true;

    /// <summary>
    /// Include product catalog content in LLMs.txt.
    /// </summary>
    public bool IncludeProductCatalog { get; set; } = true;

    /// <summary>
    /// Include blog/article content in LLMs.txt.
    /// </summary>
    public bool IncludeBlogContent { get; set; } = true;

    /// <summary>
    /// Include FAQ content in LLMs.txt.
    /// </summary>
    public bool IncludeFAQContent { get; set; } = true;

    /// <summary>
    /// Maximum content items to include in LLMs.txt.
    /// </summary>
    public int MaxContentItems { get; set; } = 1000;

    /// <summary>
    /// Content types to exclude from LLMs.txt.
    /// </summary>
    public string[] ExcludedContentTypes { get; set; } = [];

    /// <summary>
    /// Custom sections to add to LLMs.txt.
    /// Key is section name, value is content.
    /// </summary>
    public Dictionary<string, string> CustomSections { get; set; } = [];

    /// <summary>
    /// Base URL for the vector search endpoint.
    /// </summary>
    public string? VectorSearchEndpoint { get; set; }

    /// <summary>
    /// Cache duration for LLMs.txt in minutes.
    /// </summary>
    public int CacheMinutes { get; set; } = 60;
}

/// <summary>
/// Configuration for Answer Engine optimization.
/// </summary>
public class AnswerEngineOptions
{
    /// <summary>
    /// Enable automatic content summarization for AI snippets.
    /// </summary>
    public bool EnableAutoSummary { get; set; } = true;

    /// <summary>
    /// Maximum tokens for AI-generated summaries.
    /// </summary>
    public int MaxSummaryTokens { get; set; } = 200;

    /// <summary>
    /// Enable passage highlighting for featured snippets.
    /// </summary>
    public bool EnablePassageHighlighting { get; set; } = true;

    /// <summary>
    /// Target featured snippet patterns (paragraph, list, table).
    /// </summary>
    public SnippetType[] TargetSnippetTypes { get; set; } =
        [SnippetType.Paragraph, SnippetType.List, SnippetType.Table];

    /// <summary>
    /// Minimum confidence score for FAQ extraction (0.0 - 1.0).
    /// </summary>
    public double FAQExtractionConfidence { get; set; } = 0.8;

    /// <summary>
    /// Maximum FAQs to extract per page.
    /// </summary>
    public int MaxFAQsPerPage { get; set; } = 10;
}

/// <summary>
/// Configuration for SEO auditing.
/// </summary>
public class SEOAuditOptions
{
    /// <summary>
    /// Audit schedule cron expression (default: daily at 3 AM).
    /// </summary>
    public string Schedule { get; set; } = "0 3 * * *";

    /// <summary>
    /// Maximum pages to audit per run.
    /// </summary>
    public int MaxPagesPerAudit { get; set; } = 500;

    /// <summary>
    /// Enable Core Web Vitals checking.
    /// </summary>
    public bool CheckCoreWebVitals { get; set; } = true;

    /// <summary>
    /// Enable broken link detection.
    /// </summary>
    public bool CheckBrokenLinks { get; set; } = true;

    /// <summary>
    /// Enable image optimization checks.
    /// </summary>
    public bool CheckImageOptimization { get; set; } = true;

    /// <summary>
    /// Enable structured data validation.
    /// </summary>
    public bool ValidateStructuredData { get; set; } = true;

    /// <summary>
    /// Notification email for audit alerts.
    /// </summary>
    public string? NotificationEmail { get; set; }

    /// <summary>
    /// Score threshold for sending alerts.
    /// </summary>
    public int AlertThreshold { get; set; } = 50;
}

/// <summary>
/// Types of featured snippets to target.
/// </summary>
public enum SnippetType
{
    /// <summary>
    /// Paragraph snippet (40-60 words).
    /// </summary>
    Paragraph,

    /// <summary>
    /// Bulleted or numbered list.
    /// </summary>
    List,

    /// <summary>
    /// Table format for comparisons.
    /// </summary>
    Table,

    /// <summary>
    /// Video snippet with timestamp.
    /// </summary>
    Video,

    /// <summary>
    /// Step-by-step instructions.
    /// </summary>
    HowTo
}
