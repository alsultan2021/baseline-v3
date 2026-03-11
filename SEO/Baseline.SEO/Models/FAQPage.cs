using System.Text.Json;

namespace Baseline.SEO;

/// <summary>
/// FAQPage structured data (Schema.org/FAQPage).
/// </summary>
public record FAQPage
{
    /// <summary>
    /// List of question-answer pairs.
    /// </summary>
    public IReadOnlyList<QAPair> Questions { get; init; } = [];

    /// <summary>
    /// JSON-LD representation.
    /// </summary>
    public string ToJsonLd()
    {
        var items = Questions.Select(q => new Dictionary<string, object>
        {
            ["@type"] = "Question",
            ["name"] = q.Question,
            ["acceptedAnswer"] = new Dictionary<string, object>
            {
                ["@type"] = "Answer",
                ["text"] = q.Answer
            }
        });

        var schema = new Dictionary<string, object>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "FAQPage",
            ["mainEntity"] = items.ToArray()
        };

        return JsonSerializer.Serialize(schema, JsonLdDefaults.IndentedOptions);
    }
}

/// <summary>
/// A question-answer pair.
/// </summary>
public record QAPair
{
    /// <summary>
    /// The question.
    /// </summary>
    public required string Question { get; init; }

    /// <summary>
    /// The answer.
    /// </summary>
    public required string Answer { get; init; }

    /// <summary>
    /// Confidence in the extraction (0-1).
    /// </summary>
    public double Confidence { get; init; } = 1.0;

    /// <summary>
    /// Position in source content.
    /// </summary>
    public int? SourcePosition { get; init; }
}
