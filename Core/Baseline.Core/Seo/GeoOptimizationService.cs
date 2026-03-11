using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baseline.Core.Seo;

/// <summary>
/// Service for Generative Engine Optimization (GEO).
/// Optimizes content for AI search engines like Perplexity, ChatGPT, and Claude.
/// </summary>
public interface IGeoOptimizationService
{
    /// <summary>
    /// Analyzes content for GEO (Generative Engine Optimization).
    /// </summary>
    /// <param name="content">The content to analyze.</param>
    /// <returns>GEO analysis results.</returns>
    Task<GeoAnalysis> AnalyzeContentAsync(string content);

    /// <summary>
    /// Suggests improvements for AI search visibility.
    /// </summary>
    /// <param name="content">The content to analyze.</param>
    /// <returns>List of GEO suggestions.</returns>
    Task<IEnumerable<GeoSuggestion>> GetSuggestionsAsync(string content);

    /// <summary>
    /// Generates an AI-optimized content summary for search engines.
    /// </summary>
    /// <param name="content">The content to summarize.</param>
    /// <param name="maxLength">Maximum summary length in characters.</param>
    /// <returns>AI-optimized summary.</returns>
    Task<string> GenerateAiSummaryAsync(string content, int maxLength = 300);

    /// <summary>
    /// Calculates GEO score for content (0-100).
    /// </summary>
    /// <param name="content">The content to score.</param>
    /// <returns>GEO score.</returns>
    Task<int> CalculateGeoScoreAsync(string content);

    /// <summary>
    /// Extracts key entities and facts from content for AI indexing.
    /// </summary>
    /// <param name="content">The content to extract from.</param>
    /// <returns>Extracted entities and facts.</returns>
    Task<ContentEntities> ExtractEntitiesAsync(string content);
}

/// <summary>
/// GEO analysis results.
/// </summary>
public sealed record GeoAnalysis
{
    /// <summary>Overall GEO score (0-100).</summary>
    public int Score { get; init; }

    /// <summary>Whether the content is well-structured for AI.</summary>
    public bool IsAiReady { get; init; }

    /// <summary>Content readability score.</summary>
    public double ReadabilityScore { get; init; }

    /// <summary>Factual density score.</summary>
    public double FactualDensity { get; init; }

    /// <summary>Entity coverage score.</summary>
    public double EntityCoverage { get; init; }

    /// <summary>Detected content type.</summary>
    public string ContentType { get; init; } = string.Empty;

    /// <summary>Primary topics detected.</summary>
    public IReadOnlyList<string> Topics { get; init; } = [];

    /// <summary>Improvement suggestions.</summary>
    public IReadOnlyList<GeoSuggestion> Suggestions { get; init; } = [];

    /// <summary>Analysis timestamp.</summary>
    public DateTimeOffset AnalyzedAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// GEO improvement suggestion.
/// </summary>
public sealed record GeoSuggestion
{
    /// <summary>Suggestion category (e.g., "Structure", "Clarity", "Facts").</summary>
    public string Category { get; init; } = string.Empty;

    /// <summary>Priority level (High, Medium, Low).</summary>
    public GeoSuggestionPriority Priority { get; init; }

    /// <summary>The suggestion description.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>Specific action to take.</summary>
    public string Action { get; init; } = string.Empty;

    /// <summary>Expected impact on GEO score.</summary>
    public int ExpectedImpact { get; init; }
}

/// <summary>
/// GEO suggestion priority levels.
/// </summary>
public enum GeoSuggestionPriority
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Extracted entities and facts from content.
/// </summary>
public sealed record ContentEntities
{
    /// <summary>Named entities (people, organizations, locations).</summary>
    public IReadOnlyList<NamedEntity> Entities { get; init; } = [];

    /// <summary>Key facts extracted from content.</summary>
    public IReadOnlyList<string> Facts { get; init; } = [];

    /// <summary>Key dates mentioned.</summary>
    public IReadOnlyList<DateMention> Dates { get; init; } = [];

    /// <summary>Numerical data points.</summary>
    public IReadOnlyList<DataPoint> DataPoints { get; init; } = [];

    /// <summary>Questions that the content answers.</summary>
    public IReadOnlyList<string> AnsweredQuestions { get; init; } = [];
}

/// <summary>
/// A named entity extracted from content.
/// </summary>
public sealed record NamedEntity
{
    /// <summary>Entity name.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Entity type (Person, Organization, Location, Product, etc.).</summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>Confidence score (0-1).</summary>
    public double Confidence { get; init; }

    /// <summary>Number of mentions in the content.</summary>
    public int MentionCount { get; init; }
}

/// <summary>
/// A date mentioned in content.
/// </summary>
public sealed record DateMention
{
    /// <summary>The date value.</summary>
    public DateTime Date { get; init; }

    /// <summary>Context around the date mention.</summary>
    public string Context { get; init; } = string.Empty;

    /// <summary>Whether this is a significant date (event, deadline, etc.).</summary>
    public bool IsSignificant { get; init; }
}

/// <summary>
/// A numerical data point extracted from content.
/// </summary>
public sealed record DataPoint
{
    /// <summary>The value.</summary>
    public string Value { get; init; } = string.Empty;

    /// <summary>The unit or context.</summary>
    public string Unit { get; init; } = string.Empty;

    /// <summary>What this data point describes.</summary>
    public string Description { get; init; } = string.Empty;
}

/// <summary>
/// Default implementation of GEO optimization service.
/// Uses heuristics for analysis; can be enhanced with AI integration.
/// </summary>
internal sealed class GeoOptimizationService(
    IOptions<GeoOptions> options,
    ILogger<GeoOptimizationService> logger) : IGeoOptimizationService
{
    private readonly GeoOptions _options = options.Value;

    public Task<GeoAnalysis> AnalyzeContentAsync(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return Task.FromResult(new GeoAnalysis { Score = 0, IsAiReady = false });
        }

        var suggestions = new List<GeoSuggestion>();
        var topics = ExtractTopics(content);

        // Calculate component scores
        var readability = CalculateReadability(content);
        var factualDensity = CalculateFactualDensity(content);
        var entityCoverage = CalculateEntityCoverage(content);
        var structureScore = CalculateStructureScore(content);

        // Generate suggestions based on analysis
        if (readability < 0.6)
        {
            suggestions.Add(new GeoSuggestion
            {
                Category = "Readability",
                Priority = GeoSuggestionPriority.High,
                Description = "Content readability is below optimal for AI parsing",
                Action = "Use shorter sentences and simpler vocabulary",
                ExpectedImpact = 15
            });
        }

        if (factualDensity < 0.4)
        {
            suggestions.Add(new GeoSuggestion
            {
                Category = "Facts",
                Priority = GeoSuggestionPriority.Medium,
                Description = "Content lacks factual density",
                Action = "Add specific facts, statistics, and data points",
                ExpectedImpact = 10
            });
        }

        if (!content.Contains('?'))
        {
            suggestions.Add(new GeoSuggestion
            {
                Category = "Structure",
                Priority = GeoSuggestionPriority.Medium,
                Description = "Consider adding FAQ-style Q&A sections",
                Action = "Structure content as questions and answers where appropriate",
                ExpectedImpact = 12
            });
        }

        // Calculate overall score
        var score = (int)((readability * 25) + (factualDensity * 25) + (entityCoverage * 25) + (structureScore * 25));
        score = Math.Clamp(score, 0, 100);

        var analysis = new GeoAnalysis
        {
            Score = score,
            IsAiReady = score >= _options.MinimumGeoScore,
            ReadabilityScore = readability,
            FactualDensity = factualDensity,
            EntityCoverage = entityCoverage,
            ContentType = DetectContentType(content),
            Topics = topics,
            Suggestions = suggestions
        };

        logger.LogDebug("GEO analysis completed. Score: {Score}, IsAiReady: {IsAiReady}", score, analysis.IsAiReady);

        return Task.FromResult(analysis);
    }

    public async Task<IEnumerable<GeoSuggestion>> GetSuggestionsAsync(string content)
    {
        var analysis = await AnalyzeContentAsync(content);
        return analysis.Suggestions;
    }

    public Task<string> GenerateAiSummaryAsync(string content, int maxLength = 300)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return Task.FromResult(string.Empty);
        }

        // Extract first meaningful paragraph
        var paragraphs = content.Split(["\n\n", "\r\n\r\n"], StringSplitOptions.RemoveEmptyEntries);
        var summary = new System.Text.StringBuilder();

        foreach (var paragraph in paragraphs)
        {
            var cleanParagraph = paragraph.Trim();
            if (string.IsNullOrWhiteSpace(cleanParagraph) || cleanParagraph.Length < 50)
            {
                continue;
            }

            if (summary.Length + cleanParagraph.Length <= maxLength)
            {
                if (summary.Length > 0)
                {
                    summary.Append(' ');
                }
                summary.Append(cleanParagraph);
            }
            else
            {
                // Truncate to maxLength
                var remaining = maxLength - summary.Length;
                if (remaining > 50)
                {
                    if (summary.Length > 0)
                    {
                        summary.Append(' ');
                        remaining--;
                    }
                    summary.Append(cleanParagraph.AsSpan(0, Math.Min(remaining - 3, cleanParagraph.Length)));
                    summary.Append("...");
                }
                break;
            }
        }

        return Task.FromResult(summary.ToString());
    }

    public async Task<int> CalculateGeoScoreAsync(string content)
    {
        var analysis = await AnalyzeContentAsync(content);
        return analysis.Score;
    }

    public Task<ContentEntities> ExtractEntitiesAsync(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return Task.FromResult(new ContentEntities());
        }

        var entities = new List<NamedEntity>();
        var facts = new List<string>();
        var questions = new List<string>();
        var dataPoints = new List<DataPoint>();

        // Extract questions
        var sentences = content.Split(['.', '!', '?'], StringSplitOptions.RemoveEmptyEntries);
        foreach (var sentence in sentences)
        {
            var trimmed = sentence.Trim();
            if (content.Contains(trimmed + "?"))
            {
                questions.Add(trimmed + "?");
            }
        }

        // Extract numerical data points (simple pattern matching)
        var numberPattern = new System.Text.RegularExpressions.Regex(@"\b(\d+(?:,\d+)*(?:\.\d+)?)\s*(%|dollars?|euros?|pounds?|USD|EUR|GBP|years?|months?|days?|hours?|minutes?|kg|lbs?|miles?|km|meters?)\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        foreach (System.Text.RegularExpressions.Match match in numberPattern.Matches(content))
        {
            dataPoints.Add(new DataPoint
            {
                Value = match.Groups[1].Value,
                Unit = match.Groups[2].Value,
                Description = ExtractContext(content, match.Index, 50)
            });
        }

        // Extract sentences with factual indicators
        var factIndicators = new[] { "is", "are", "was", "were", "has", "have", "according to", "research shows", "studies indicate" };
        foreach (var sentence in sentences)
        {
            var trimmed = sentence.Trim();
            if (trimmed.Length > 20 && factIndicators.Any(f => trimmed.Contains(f, StringComparison.OrdinalIgnoreCase)))
            {
                facts.Add(trimmed + ".");
                if (facts.Count >= 10) break; // Limit to 10 facts
            }
        }

        return Task.FromResult(new ContentEntities
        {
            Entities = entities,
            Facts = facts,
            AnsweredQuestions = questions,
            DataPoints = dataPoints
        });
    }

    private static double CalculateReadability(string content)
    {
        var sentences = content.Split(['.', '!', '?'], StringSplitOptions.RemoveEmptyEntries);
        var words = content.Split([' ', '\t', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries);

        if (sentences.Length == 0 || words.Length == 0) return 0;

        var avgWordsPerSentence = (double)words.Length / sentences.Length;
        var avgWordLength = words.Average(w => w.Length);

        // Optimal: 15-20 words per sentence, 4-6 chars per word
        var sentenceScore = avgWordsPerSentence switch
        {
            < 10 => 0.7,
            <= 20 => 1.0,
            <= 25 => 0.8,
            _ => 0.5
        };

        var wordScore = avgWordLength switch
        {
            < 4 => 0.8,
            <= 6 => 1.0,
            <= 8 => 0.7,
            _ => 0.5
        };

        return (sentenceScore + wordScore) / 2;
    }

    private static double CalculateFactualDensity(string content)
    {
        var factIndicators = new[] { "is", "are", "was", "were", "founded", "located", "established", "created", "according to" };
        var sentences = content.Split(['.', '!', '?'], StringSplitOptions.RemoveEmptyEntries);
        if (sentences.Length == 0) return 0;

        var factualSentences = sentences.Count(s => factIndicators.Any(f => s.Contains(f, StringComparison.OrdinalIgnoreCase)));
        return (double)factualSentences / sentences.Length;
    }

    private static double CalculateEntityCoverage(string content)
    {
        // Check for proper nouns (simple heuristic: words starting with uppercase not at sentence start)
        var words = content.Split([' ', '\t', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
        if (words.Length < 10) return 0;

        var capitalizedWords = 0;
        for (int i = 1; i < words.Length; i++)
        {
            if (words[i].Length > 1 && char.IsUpper(words[i][0]) && !words[i - 1].EndsWith('.'))
            {
                capitalizedWords++;
            }
        }

        var ratio = (double)capitalizedWords / words.Length;
        return Math.Min(ratio * 10, 1.0); // Normalize to 0-1
    }

    private static double CalculateStructureScore(string content)
    {
        var score = 0.5; // Base score

        // Check for headings (markdown-style)
        if (content.Contains('#')) score += 0.1;

        // Check for lists
        if (content.Contains("- ") || content.Contains("* ") || content.Contains("1.")) score += 0.1;

        // Check for paragraphs
        var paragraphs = content.Split(["\n\n", "\r\n\r\n"], StringSplitOptions.RemoveEmptyEntries);
        if (paragraphs.Length >= 3) score += 0.1;

        // Check for questions (FAQ-style)
        if (content.Contains('?')) score += 0.1;

        // Check for links
        if (content.Contains("http") || content.Contains('[')) score += 0.1;

        return Math.Min(score, 1.0);
    }

    private static List<string> ExtractTopics(string content)
    {
        // Simple topic extraction based on word frequency
        var words = content.ToLowerInvariant()
            .Split([' ', '\t', '\n', '\r', '.', ',', '!', '?', ';', ':', '"', '\'', '(', ')', '[', ']'], StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 4)
            .Where(w => !StopWords.Contains(w))
            .GroupBy(w => w)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => g.Key)
            .ToList();

        return words;
    }

    private static string DetectContentType(string content)
    {
        var lower = content.ToLowerInvariant();

        if (lower.Contains("how to") || lower.Contains("step 1") || lower.Contains("steps to"))
            return "HowTo";
        if (lower.Contains("frequently asked") || lower.Contains("faq") || content.Count(c => c == '?') > 3)
            return "FAQ";
        if (lower.Contains("recipe") || lower.Contains("ingredients") || lower.Contains("cook"))
            return "Recipe";
        if (lower.Contains("review") || lower.Contains("rating") || lower.Contains("stars"))
            return "Review";
        if (lower.Contains("event") || lower.Contains("when:") || lower.Contains("where:"))
            return "Event";

        return "Article";
    }

    private static string ExtractContext(string content, int position, int contextLength)
    {
        var start = Math.Max(0, position - contextLength);
        var end = Math.Min(content.Length, position + contextLength);
        return content[start..end].Trim();
    }

    private static readonly HashSet<string> StopWords =
    [
        "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for", "of", "with", "by",
        "from", "as", "is", "was", "are", "were", "been", "be", "have", "has", "had", "do", "does",
        "did", "will", "would", "could", "should", "may", "might", "must", "shall", "can", "need",
        "this", "that", "these", "those", "it", "its", "they", "them", "their", "we", "our", "you",
        "your", "he", "she", "him", "her", "his", "hers", "which", "who", "whom", "what", "when",
        "where", "why", "how", "all", "each", "every", "both", "few", "more", "most", "other",
        "some", "such", "only", "own", "same", "than", "too", "very", "just", "also", "now"
    ];
}

/// <summary>
/// Configuration options for GEO optimization.
/// </summary>
public sealed class GeoOptions
{
    /// <summary>Minimum GEO score to be considered AI-ready (0-100).</summary>
    public int MinimumGeoScore { get; set; } = 60;

    /// <summary>Enable automatic GEO analysis on content save.</summary>
    public bool EnableAutoAnalysis { get; set; } = false;

    /// <summary>Enable GEO suggestions in admin UI.</summary>
    public bool EnableSuggestions { get; set; } = true;

    /// <summary>Maximum number of topics to extract.</summary>
    public int MaxTopics { get; set; } = 5;

    /// <summary>Maximum number of facts to extract.</summary>
    public int MaxFacts { get; set; } = 10;
}
