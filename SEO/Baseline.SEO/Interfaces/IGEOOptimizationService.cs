namespace Baseline.SEO;

/// <summary>
/// Service for GEO (Generative Engine Optimization).
/// Optimizes content for AI-powered search engines like Perplexity, ChatGPT, Claude, etc.
/// </summary>
/// <remarks>
/// <b>No Core overlap.</b> This is a purely AI-powered service with no equivalent in
/// Baseline.Core. Core handles traditional SEO (meta tags, structured data); GEO handles
/// next-gen AI search visibility (content analysis, citability, concept variations).
/// </remarks>
public interface IGEOOptimizationService
{
    /// <summary>
    /// Analyzes content for GEO optimization.
    /// Checks for AI-friendly content structure, clarity, and citability.
    /// </summary>
    /// <param name="content">The content to analyze.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>GEO analysis results with score and suggestions.</returns>
    Task<GEOAnalysis> AnalyzeContentAsync(
        string content,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyzes a page by URL for GEO optimization.
    /// </summary>
    /// <param name="url">The URL to analyze.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>GEO analysis results.</returns>
    Task<GEOAnalysis> AnalyzePageAsync(
        string url,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Suggests improvements for AI search visibility.
    /// </summary>
    /// <param name="content">The content to improve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of suggestions for improvement.</returns>
    Task<IEnumerable<GEOSuggestion>> GetSuggestionsAsync(
        string content,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates an AI-optimized content summary.
    /// Creates a concise summary ideal for AI citation.
    /// </summary>
    /// <param name="content">The content to summarize.</param>
    /// <param name="maxTokens">Maximum tokens for the summary.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>AI-optimized summary.</returns>
    Task<string> GenerateAISummaryAsync(
        string content,
        int maxTokens = 200,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if content is suitable for AI citation.
    /// </summary>
    /// <param name="content">The content to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Citability assessment.</returns>
    Task<CitabilityResult> CheckCitabilityAsync(
        string content,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates alternative phrasings for key concepts.
    /// Helps AI models understand content from multiple angles.
    /// </summary>
    /// <param name="content">The content to analyze.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Alternative phrasings for key concepts.</returns>
    Task<IEnumerable<ConceptVariation>> GenerateConceptVariationsAsync(
        string content,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Complete GEO analysis result.
/// </summary>
public record GEOAnalysis
{
    /// <summary>
    /// Overall GEO score (0-100).
    /// </summary>
    public int Score { get; init; }

    /// <summary>
    /// Score breakdown by category.
    /// </summary>
    public GEOScoreBreakdown Breakdown { get; init; } = new();

    /// <summary>
    /// Analysis findings.
    /// </summary>
    public IReadOnlyList<GEOFinding> Findings { get; init; } = [];

    /// <summary>
    /// Improvement suggestions.
    /// </summary>
    public IReadOnlyList<GEOSuggestion> Suggestions { get; init; } = [];

    /// <summary>
    /// Detected content patterns suitable for structured data.
    /// </summary>
    public IReadOnlyList<ContentPattern> DetectedPatterns { get; init; } = [];

    /// <summary>
    /// Key topics/entities detected in content.
    /// </summary>
    public IReadOnlyList<string> DetectedTopics { get; init; } = [];

    /// <summary>
    /// Estimated AI citation probability.
    /// </summary>
    public double CitationProbability { get; init; }

    /// <summary>
    /// Analysis timestamp.
    /// </summary>
    public DateTime AnalyzedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Breakdown of GEO score by category.
/// </summary>
public record GEOScoreBreakdown
{
    /// <summary>
    /// Content clarity and readability (0-100).
    /// </summary>
    public int Clarity { get; init; }

    /// <summary>
    /// Content structure and formatting (0-100).
    /// </summary>
    public int Structure { get; init; }

    /// <summary>
    /// Factual accuracy indicators (0-100).
    /// </summary>
    public int Authority { get; init; }

    /// <summary>
    /// Comprehensiveness of topic coverage (0-100).
    /// </summary>
    public int Comprehensiveness { get; init; }

    /// <summary>
    /// Freshness and recency signals (0-100).
    /// </summary>
    public int Freshness { get; init; }

    /// <summary>
    /// Technical SEO factors (0-100).
    /// </summary>
    public int Technical { get; init; }
}

/// <summary>
/// Result of citability check.
/// </summary>
public record CitabilityResult
{
    /// <summary>
    /// Whether content is suitable for AI citation.
    /// </summary>
    public bool IsCitable { get; init; }

    /// <summary>
    /// Citability score (0-100).
    /// </summary>
    public int Score { get; init; }

    /// <summary>
    /// Key quotable passages.
    /// </summary>
    public IReadOnlyList<string> QuotablePassages { get; init; } = [];

    /// <summary>
    /// Issues that reduce citability.
    /// </summary>
    public IReadOnlyList<string> Issues { get; init; } = [];

    /// <summary>
    /// Detected facts and statistics.
    /// </summary>
    public IReadOnlyList<FactStatement> Facts { get; init; } = [];
}

/// <summary>
/// A factual statement detected in content.
/// </summary>
public record FactStatement
{
    /// <summary>
    /// The factual statement.
    /// </summary>
    public required string Statement { get; init; }

    /// <summary>
    /// Confidence in the extraction (0-1).
    /// </summary>
    public double Confidence { get; init; }

    /// <summary>
    /// Whether this appears to be a statistic.
    /// </summary>
    public bool IsStatistic { get; init; }

    /// <summary>
    /// Source attribution if present.
    /// </summary>
    public string? Source { get; init; }
}

/// <summary>
/// Alternative phrasing for a concept.
/// </summary>
public record ConceptVariation
{
    /// <summary>
    /// The original concept/phrase.
    /// </summary>
    public required string Original { get; init; }

    /// <summary>
    /// Alternative phrasings.
    /// </summary>
    public IReadOnlyList<string> Variations { get; init; } = [];

    /// <summary>
    /// Related questions users might ask.
    /// </summary>
    public IReadOnlyList<string> RelatedQuestions { get; init; } = [];
}
