using System.Text.Json;

namespace Baseline.SEO;

/// <summary>
/// Collection of structured data for a page.
/// </summary>
public record StructuredDataCollection
{
    /// <summary>
    /// All structured data items.
    /// </summary>
    public IReadOnlyList<StructuredDataItem> Items { get; init; } = [];

    /// <summary>
    /// Generates combined JSON-LD with @graph.
    /// </summary>
    public string ToJsonLd()
    {
        if (Items.Count == 0)
            return string.Empty;

        if (Items.Count == 1)
            return Items[0].JsonLd;

        var graph = Items.Select(i =>
            JsonSerializer.Deserialize<object>(i.JsonLd));

        var combined = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@graph"] = graph.ToArray()
        };

        return JsonSerializer.Serialize(combined, JsonLdDefaults.IndentedOptions);
    }
}

/// <summary>
/// A single structured data item.
/// </summary>
public record StructuredDataItem
{
    /// <summary>
    /// Schema.org type (e.g., "FAQPage", "HowTo", "Article").
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// JSON-LD string.
    /// </summary>
    public required string JsonLd { get; init; }

    /// <summary>
    /// Confidence in the structured data.
    /// </summary>
    public double Confidence { get; init; } = 1.0;
}
