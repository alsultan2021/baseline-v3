using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Baseline.Core.Seo;

/// <summary>
/// Service for optimizing content for AI Answer Engines.
/// Generates structured data for featured snippets, direct answers, and AI summaries.
/// </summary>
public interface IAnswerEngineService
{
    /// <summary>
    /// Extracts FAQ schema from content.
    /// </summary>
    /// <param name="content">The content to extract FAQs from.</param>
    /// <returns>FAQ page structured data.</returns>
    Task<FaqPage> ExtractFaqsAsync(string content);

    /// <summary>
    /// Generates HowTo schema from instructional content.
    /// </summary>
    /// <param name="content">The instructional content.</param>
    /// <param name="title">The title of the how-to guide.</param>
    /// <returns>HowTo structured data.</returns>
    Task<HowTo> GenerateHowToAsync(string content, string title);

    /// <summary>
    /// Creates Speakable schema for voice search optimization.
    /// </summary>
    /// <param name="content">The content to make speakable.</param>
    /// <param name="cssSelectors">CSS selectors for speakable sections.</param>
    /// <returns>Speakable structured data.</returns>
    Task<Speakable> CreateSpeakableAsync(string content, IEnumerable<string>? cssSelectors = null);

    /// <summary>
    /// Generates a direct answer snippet optimized for AI engines.
    /// </summary>
    /// <param name="question">The question to answer.</param>
    /// <param name="content">The content containing the answer.</param>
    /// <returns>Direct answer data.</returns>
    Task<DirectAnswer> GenerateDirectAnswerAsync(string question, string content);

    /// <summary>
    /// Creates a knowledge panel structure from entity information.
    /// </summary>
    /// <param name="entity">The entity information.</param>
    /// <returns>Knowledge panel structured data.</returns>
    Task<KnowledgePanel> CreateKnowledgePanelAsync(EntityInfo entity);
}

/// <summary>
/// FAQ Page structured data (Schema.org FAQPage).
/// </summary>
public sealed record FaqPage
{
    [JsonPropertyName("@context")]
    public string Context { get; init; } = "https://schema.org";

    [JsonPropertyName("@type")]
    public string Type { get; init; } = "FAQPage";

    [JsonPropertyName("mainEntity")]
    public IReadOnlyList<Question> MainEntity { get; init; } = [];

    /// <summary>Generates JSON-LD script tag.</summary>
    public string ToJsonLd() => $"<script type=\"application/ld+json\">{JsonSerializer.Serialize(this, JsonOptions)}</script>";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}

/// <summary>
/// A question in FAQ schema.
/// </summary>
public sealed record Question
{
    [JsonPropertyName("@type")]
    public string Type { get; init; } = "Question";

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("acceptedAnswer")]
    public Answer AcceptedAnswer { get; init; } = new();
}

/// <summary>
/// An answer in FAQ schema.
/// </summary>
public sealed record Answer
{
    [JsonPropertyName("@type")]
    public string Type { get; init; } = "Answer";

    [JsonPropertyName("text")]
    public string Text { get; init; } = string.Empty;
}

/// <summary>
/// HowTo structured data (Schema.org HowTo).
/// </summary>
public sealed record HowTo
{
    [JsonPropertyName("@context")]
    public string Context { get; init; } = "https://schema.org";

    [JsonPropertyName("@type")]
    public string Type { get; init; } = "HowTo";

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("totalTime")]
    public string? TotalTime { get; init; }

    [JsonPropertyName("estimatedCost")]
    public MonetaryAmount? EstimatedCost { get; init; }

    [JsonPropertyName("supply")]
    public IReadOnlyList<HowToSupply>? Supply { get; init; }

    [JsonPropertyName("tool")]
    public IReadOnlyList<HowToTool>? Tool { get; init; }

    [JsonPropertyName("step")]
    public IReadOnlyList<HowToStep> Step { get; init; } = [];

    /// <summary>Generates JSON-LD script tag.</summary>
    public string ToJsonLd() => $"<script type=\"application/ld+json\">{JsonSerializer.Serialize(this, JsonOptions)}</script>";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}

/// <summary>
/// A step in HowTo schema.
/// </summary>
public sealed record HowToStep
{
    [JsonPropertyName("@type")]
    public string Type { get; init; } = "HowToStep";

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("text")]
    public string Text { get; init; } = string.Empty;

    [JsonPropertyName("url")]
    public string? Url { get; init; }

    [JsonPropertyName("image")]
    public string? Image { get; init; }
}

/// <summary>
/// A supply needed for HowTo.
/// </summary>
public sealed record HowToSupply
{
    [JsonPropertyName("@type")]
    public string Type { get; init; } = "HowToSupply";

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;
}

/// <summary>
/// A tool needed for HowTo.
/// </summary>
public sealed record HowToTool
{
    [JsonPropertyName("@type")]
    public string Type { get; init; } = "HowToTool";

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;
}

/// <summary>
/// Monetary amount for cost estimation.
/// </summary>
public sealed record MonetaryAmount
{
    [JsonPropertyName("@type")]
    public string Type { get; init; } = "MonetaryAmount";

    [JsonPropertyName("currency")]
    public string Currency { get; init; } = "USD";

    [JsonPropertyName("value")]
    public string Value { get; init; } = "0";
}

/// <summary>
/// Speakable structured data for voice search.
/// </summary>
public sealed record Speakable
{
    [JsonPropertyName("@context")]
    public string Context { get; init; } = "https://schema.org";

    [JsonPropertyName("@type")]
    public string Type { get; init; } = "WebPage";

    [JsonPropertyName("speakable")]
    public SpeakableSpecification SpeakableSpec { get; init; } = new();

    /// <summary>Generates JSON-LD script tag.</summary>
    public string ToJsonLd() => $"<script type=\"application/ld+json\">{JsonSerializer.Serialize(this, JsonOptions)}</script>";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}

/// <summary>
/// Speakable specification.
/// </summary>
public sealed record SpeakableSpecification
{
    [JsonPropertyName("@type")]
    public string Type { get; init; } = "SpeakableSpecification";

    [JsonPropertyName("cssSelector")]
    public IReadOnlyList<string>? CssSelector { get; init; }

    [JsonPropertyName("xpath")]
    public IReadOnlyList<string>? XPath { get; init; }
}

/// <summary>
/// Direct answer optimized for AI engines.
/// </summary>
public sealed record DirectAnswer
{
    /// <summary>The question being answered.</summary>
    public string Question { get; init; } = string.Empty;

    /// <summary>The direct answer text.</summary>
    public string Answer { get; init; } = string.Empty;

    /// <summary>Confidence score (0-1).</summary>
    public double Confidence { get; init; }

    /// <summary>Source content excerpt.</summary>
    public string SourceExcerpt { get; init; } = string.Empty;

    /// <summary>Answer type (Definition, Fact, List, Comparison).</summary>
    public string AnswerType { get; init; } = "Fact";
}

/// <summary>
/// Knowledge panel structure.
/// </summary>
public sealed record KnowledgePanel
{
    /// <summary>Entity name.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Entity type.</summary>
    public string EntityType { get; init; } = string.Empty;

    /// <summary>Short description.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>Image URL.</summary>
    public string? ImageUrl { get; init; }

    /// <summary>Key facts about the entity.</summary>
    public IReadOnlyDictionary<string, string> Facts { get; init; } = new Dictionary<string, string>();

    /// <summary>Related entities.</summary>
    public IReadOnlyList<string> RelatedEntities { get; init; } = [];

    /// <summary>Official website.</summary>
    public string? Website { get; init; }

    /// <summary>Social profiles.</summary>
    public IReadOnlyDictionary<string, string> SocialProfiles { get; init; } = new Dictionary<string, string>();
}

/// <summary>
/// Entity information for knowledge panel generation.
/// </summary>
public sealed record EntityInfo
{
    /// <summary>Entity name.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Entity type (Person, Organization, Place, Product).</summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>Entity description.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>Image URL.</summary>
    public string? ImageUrl { get; init; }

    /// <summary>Additional properties.</summary>
    public IReadOnlyDictionary<string, string> Properties { get; init; } = new Dictionary<string, string>();

    /// <summary>Website URL.</summary>
    public string? Website { get; init; }

    /// <summary>Social media URLs.</summary>
    public IReadOnlyDictionary<string, string> SocialProfiles { get; init; } = new Dictionary<string, string>();
}

/// <summary>
/// Default implementation of Answer Engine Service.
/// </summary>
internal sealed class AnswerEngineService(ILogger<AnswerEngineService> logger) : IAnswerEngineService
{
    public Task<FaqPage> ExtractFaqsAsync(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return Task.FromResult(new FaqPage());
        }

        var questions = new List<Question>();
        var lines = content.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();

            // Look for questions (lines ending with ?)
            if (line.EndsWith('?'))
            {
                var questionText = line.TrimStart('#', ' ', '*', '-');
                var answerText = new System.Text.StringBuilder();

                // Collect answer from following lines until next question or empty line
                for (int j = i + 1; j < lines.Length; j++)
                {
                    var answerLine = lines[j].Trim();
                    if (string.IsNullOrWhiteSpace(answerLine) || answerLine.EndsWith('?'))
                    {
                        break;
                    }
                    if (answerText.Length > 0) answerText.Append(' ');
                    answerText.Append(answerLine.TrimStart('#', ' ', '*', '-'));
                }

                if (answerText.Length > 0)
                {
                    questions.Add(new Question
                    {
                        Name = questionText,
                        AcceptedAnswer = new Answer { Text = answerText.ToString() }
                    });
                }
            }
        }

        logger.LogDebug("Extracted {Count} FAQ questions from content", questions.Count);

        return Task.FromResult(new FaqPage { MainEntity = questions });
    }

    public Task<HowTo> GenerateHowToAsync(string content, string title)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return Task.FromResult(new HowTo { Name = title });
        }

        var steps = new List<HowToStep>();
        var lines = content.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
        var stepNumber = 0;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            // Look for numbered steps or bullet points
            if (IsStepIndicator(trimmed, out var stepText))
            {
                stepNumber++;
                steps.Add(new HowToStep
                {
                    Name = $"Step {stepNumber}",
                    Text = stepText
                });
            }
        }

        // If no steps found, try to split by sentences
        if (steps.Count == 0)
        {
            var sentences = content.Split(['.'], StringSplitOptions.RemoveEmptyEntries)
                .Where(s => s.Trim().Length > 20)
                .Take(10);

            foreach (var sentence in sentences)
            {
                stepNumber++;
                steps.Add(new HowToStep
                {
                    Name = $"Step {stepNumber}",
                    Text = sentence.Trim() + "."
                });
            }
        }

        logger.LogDebug("Generated HowTo with {Count} steps", steps.Count);

        return Task.FromResult(new HowTo
        {
            Name = title,
            Description = content.Length > 200 ? content[..200] + "..." : content,
            Step = steps
        });
    }

    public Task<Speakable> CreateSpeakableAsync(string content, IEnumerable<string>? cssSelectors = null)
    {
        var selectors = cssSelectors?.ToList() ?? ["article", "h1", ".summary", ".intro"];

        return Task.FromResult(new Speakable
        {
            SpeakableSpec = new SpeakableSpecification
            {
                CssSelector = selectors
            }
        });
    }

    public Task<DirectAnswer> GenerateDirectAnswerAsync(string question, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return Task.FromResult(new DirectAnswer { Question = question, Confidence = 0 });
        }

        // Find the most relevant sentence that could answer the question
        var sentences = content.Split(['.', '!'], StringSplitOptions.RemoveEmptyEntries);
        var questionWords = ExtractKeywords(question);

        string bestAnswer = string.Empty;
        double bestScore = 0;

        foreach (var sentence in sentences)
        {
            var trimmed = sentence.Trim();
            if (trimmed.Length < 20) continue;

            var sentenceWords = ExtractKeywords(trimmed);
            var matchCount = questionWords.Intersect(sentenceWords, StringComparer.OrdinalIgnoreCase).Count();
            var score = (double)matchCount / Math.Max(questionWords.Count, 1);

            if (score > bestScore)
            {
                bestScore = score;
                bestAnswer = trimmed + ".";
            }
        }

        var answerType = DetermineAnswerType(question);

        return Task.FromResult(new DirectAnswer
        {
            Question = question,
            Answer = bestAnswer,
            Confidence = bestScore,
            SourceExcerpt = bestAnswer.Length > 100 ? bestAnswer[..100] + "..." : bestAnswer,
            AnswerType = answerType
        });
    }

    public Task<KnowledgePanel> CreateKnowledgePanelAsync(EntityInfo entity)
    {
        return Task.FromResult(new KnowledgePanel
        {
            Name = entity.Name,
            EntityType = entity.Type,
            Description = entity.Description,
            ImageUrl = entity.ImageUrl,
            Facts = entity.Properties,
            Website = entity.Website,
            SocialProfiles = entity.SocialProfiles
        });
    }

    private static bool IsStepIndicator(string line, out string stepText)
    {
        stepText = line;

        // Check for numbered steps: "1.", "1)", "Step 1:", etc.
        var numberMatch = System.Text.RegularExpressions.Regex.Match(line, @"^(?:step\s*)?(\d+)[.):]\s*(.+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (numberMatch.Success)
        {
            stepText = numberMatch.Groups[2].Value.Trim();
            return true;
        }

        // Check for bullet points: "- ", "* ", "• "
        if (line.StartsWith("- ") || line.StartsWith("* ") || line.StartsWith("• "))
        {
            stepText = line[2..].Trim();
            return true;
        }

        return false;
    }

    private static List<string> ExtractKeywords(string text)
    {
        return text.ToLowerInvariant()
            .Split([' ', '\t', ',', '.', '!', '?', ';', ':', '"', '\''], StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 3)
            .Where(w => !StopWords.Contains(w))
            .Distinct()
            .ToList();
    }

    private static string DetermineAnswerType(string question)
    {
        var lower = question.ToLowerInvariant();

        if (lower.StartsWith("what is") || lower.StartsWith("what are") || lower.Contains("definition"))
            return "Definition";
        if (lower.StartsWith("how many") || lower.StartsWith("how much") || lower.Contains("number of"))
            return "Quantity";
        if (lower.StartsWith("when") || lower.Contains("date") || lower.Contains("time"))
            return "DateTime";
        if (lower.StartsWith("where") || lower.Contains("location"))
            return "Location";
        if (lower.StartsWith("who"))
            return "Person";
        if (lower.Contains("compare") || lower.Contains("difference") || lower.Contains("versus") || lower.Contains("vs"))
            return "Comparison";
        if (lower.StartsWith("how to") || lower.StartsWith("how do"))
            return "HowTo";
        if (lower.Contains("list") || lower.Contains("examples"))
            return "List";

        return "Fact";
    }

    private static readonly HashSet<string> StopWords =
    [
        "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for", "of", "with", "by",
        "from", "as", "is", "was", "are", "were", "been", "be", "have", "has", "had", "do", "does",
        "did", "will", "would", "could", "should", "may", "might", "must", "shall", "can", "need",
        "this", "that", "these", "those", "it", "its", "they", "them", "their", "what", "which",
        "who", "whom", "when", "where", "why", "how"
    ];
}
