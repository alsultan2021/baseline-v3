using System.Text.Json;

namespace Baseline.SEO;

/// <summary>
/// Speakable content for voice search (Schema.org/speakable).
/// </summary>
public record Speakable
{
    /// <summary>
    /// CSS selectors for speakable content.
    /// </summary>
    public IReadOnlyList<string> CssSelectors { get; init; } = [];

    /// <summary>
    /// XPath expressions for speakable content.
    /// </summary>
    public IReadOnlyList<string> XPaths { get; init; } = [];

    /// <summary>
    /// Direct text content that is speakable.
    /// </summary>
    public IReadOnlyList<string> SpeakableText { get; init; } = [];

    /// <summary>
    /// Generates JSON-LD representation.
    /// </summary>
    public string ToJsonLd(string pageUrl)
    {
        var schema = new Dictionary<string, object>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "WebPage",
            ["url"] = pageUrl,
            ["speakable"] = new Dictionary<string, object>
            {
                ["@type"] = "SpeakableSpecification",
                ["cssSelector"] = CssSelectors.ToArray()
            }
        };

        return JsonSerializer.Serialize(schema, JsonLdDefaults.IndentedOptions);
    }
}
