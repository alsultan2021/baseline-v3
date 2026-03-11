using Microsoft.Extensions.DependencyInjection;

namespace Baseline.SEO;

/// <summary>
/// Builder interface for configuring Baseline SEO services.
/// Follows the same pattern as ILuceneBuilder and IAIBuilder.
/// </summary>
public interface ISEOBuilder
{
    /// <summary>
    /// The service collection being configured.
    /// </summary>
    IServiceCollection Services { get; }

    /// <summary>
    /// Registers a custom GEO analyzer.
    /// </summary>
    /// <typeparam name="TAnalyzer">The GEO analyzer type.</typeparam>
    /// <returns>The builder for chaining.</returns>
    ISEOBuilder RegisterGEOAnalyzer<TAnalyzer>() where TAnalyzer : class, IGEOAnalyzer;

    /// <summary>
    /// Registers a custom SEO auditor.
    /// </summary>
    /// <typeparam name="TAuditor">The SEO auditor type.</typeparam>
    /// <returns>The builder for chaining.</returns>
    ISEOBuilder RegisterAuditor<TAuditor>() where TAuditor : class, ISEOAuditor;

    /// <summary>
    /// Registers a custom structured data generator.
    /// </summary>
    /// <typeparam name="TGenerator">The structured data generator type.</typeparam>
    /// <param name="schemaType">The Schema.org type this generator handles.</param>
    /// <returns>The builder for chaining.</returns>
    ISEOBuilder RegisterSchemaGenerator<TGenerator>(string schemaType)
        where TGenerator : class, ISchemaGenerator;

    /// <summary>
    /// Registers a custom LLMs.txt section provider.
    /// </summary>
    /// <typeparam name="TProvider">The LLMs section provider type.</typeparam>
    /// <returns>The builder for chaining.</returns>
    ISEOBuilder RegisterLLMsSectionProvider<TProvider>()
        where TProvider : class, ILLMsSectionProvider;

    /// <summary>
    /// Configures SEO options.
    /// </summary>
    /// <param name="configure">Configuration action.</param>
    /// <returns>The builder for chaining.</returns>
    ISEOBuilder Configure(Action<BaselineSEOOptions> configure);

    /// <summary>
    /// Include default auditors (meta tags, headings, images, links).
    /// </summary>
    bool IncludeDefaultAuditors { get; set; }
}

/// <summary>
/// Interface for custom GEO content analyzers.
/// </summary>
public interface IGEOAnalyzer
{
    /// <summary>
    /// Analyzes content for GEO optimization.
    /// </summary>
    /// <param name="content">The content to analyze.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Analysis results.</returns>
    Task<GEOAnalysisResult> AnalyzeAsync(string content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Priority of this analyzer (higher = runs first).
    /// </summary>
    int Priority { get; }
}

/// <summary>
/// Interface for custom SEO auditors.
/// </summary>
public interface ISEOAuditor
{
    /// <summary>
    /// Unique identifier for this auditor.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Display name for audit reports.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Audits a page and returns findings.
    /// </summary>
    /// <param name="context">The audit context with page information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Audit findings.</returns>
    Task<IEnumerable<SEOAuditFinding>> AuditAsync(
        SEOAuditContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Maximum score contribution of this auditor.
    /// </summary>
    int MaxScore { get; }
}

/// <summary>
/// Interface for Schema.org structured data generators.
/// </summary>
public interface ISchemaGenerator
{
    /// <summary>
    /// Schema.org type this generator handles (e.g., "Article", "Product").
    /// </summary>
    string SchemaType { get; }

    /// <summary>
    /// Generates structured data for content.
    /// </summary>
    /// <param name="content">The content item.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON-LD structured data.</returns>
    Task<string?> GenerateAsync(object content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Whether this generator can handle the content.
    /// </summary>
    bool CanHandle(object content);
}

/// <summary>
/// Interface for custom LLMs.txt section providers.
/// </summary>
public interface ILLMsSectionProvider
{
    /// <summary>
    /// Section name in LLMs.txt.
    /// </summary>
    string SectionName { get; }

    /// <summary>
    /// Priority of this section (higher = appears first).
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Generates section content.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Section content.</returns>
    Task<string> GenerateSectionAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of GEO content analysis.
/// </summary>
public record GEOAnalysisResult
{
    /// <summary>
    /// Overall GEO score (0-100).
    /// </summary>
    public int Score { get; init; }

    /// <summary>
    /// Findings from the analysis.
    /// </summary>
    public IReadOnlyList<GEOFinding> Findings { get; init; } = [];

    /// <summary>
    /// Suggested improvements.
    /// </summary>
    public IReadOnlyList<GEOSuggestion> Suggestions { get; init; } = [];

    /// <summary>
    /// Detected content patterns.
    /// </summary>
    public IReadOnlyList<ContentPattern> Patterns { get; init; } = [];
}

/// <summary>
/// A finding from GEO analysis.
/// </summary>
public record GEOFinding
{
    public required string Category { get; init; }
    public required string Message { get; init; }
    public GEOFindingSeverity Severity { get; init; }
    public string? Location { get; init; }
}

/// <summary>
/// Severity of a GEO finding.
/// </summary>
public enum GEOFindingSeverity
{
    Info,
    Warning,
    Error
}

/// <summary>
/// A suggestion for GEO improvement.
/// </summary>
public record GEOSuggestion
{
    public required string Title { get; init; }
    public required string Description { get; init; }
    public int ImpactScore { get; init; }
    public string? Example { get; init; }
}

/// <summary>
/// A detected content pattern for structured data.
/// </summary>
public record ContentPattern
{
    public required string PatternType { get; init; }
    public required string Content { get; init; }
    public double Confidence { get; init; }
    public int StartPosition { get; init; }
    public int EndPosition { get; init; }
}

/// <summary>
/// Context for SEO auditing.
/// </summary>
public record SEOAuditContext
{
    public required string Url { get; init; }
    public required string HtmlContent { get; init; }
    public string? Title { get; init; }
    public string? MetaDescription { get; init; }
    public IDictionary<string, string> MetaTags { get; init; } = new Dictionary<string, string>();
    public IReadOnlyList<string> Headings { get; init; } = [];
    public IReadOnlyList<ImageInfo> Images { get; init; } = [];
    public IReadOnlyList<LinkInfo> Links { get; init; } = [];
    public string? CanonicalUrl { get; init; }
    public string? ContentType { get; init; }
    public int? ContentItemId { get; init; }
}

/// <summary>
/// Information about an image on the page.
/// </summary>
public record ImageInfo
{
    public required string Src { get; init; }
    public string? Alt { get; init; }
    public int? Width { get; init; }
    public int? Height { get; init; }
    public bool HasLazyLoading { get; init; }
}

/// <summary>
/// Information about a link on the page.
/// </summary>
public record LinkInfo
{
    public required string Href { get; init; }
    public string? Text { get; init; }
    public bool IsExternal { get; init; }
    public bool HasNoFollow { get; init; }
    public bool HasNoOpener { get; init; }
}

/// <summary>
/// A finding from SEO audit.
/// </summary>
public record SEOAuditFinding
{
    public required string AuditorId { get; init; }
    public required string Category { get; init; }
    public required string Message { get; init; }
    public SEOAuditSeverity Severity { get; init; }
    public int ScoreImpact { get; init; }
    public string? Recommendation { get; init; }
    public string? Element { get; init; }
}

/// <summary>
/// Severity of an SEO audit finding.
/// </summary>
public enum SEOAuditSeverity
{
    /// <summary>
    /// Informational finding.
    /// </summary>
    Info,

    /// <summary>
    /// Minor issue that should be addressed.
    /// </summary>
    Warning,

    /// <summary>
    /// Significant issue affecting SEO.
    /// </summary>
    Error,

    /// <summary>
    /// Critical issue requiring immediate attention.
    /// </summary>
    Critical
}
