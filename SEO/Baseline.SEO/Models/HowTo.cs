using System.Text.Json;

namespace Baseline.SEO;

/// <summary>
/// HowTo structured data (Schema.org/HowTo).
/// </summary>
public record HowTo
{
    /// <summary>
    /// Name/title of the how-to guide.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Description of what this how-to accomplishes.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Estimated total time (ISO 8601 duration).
    /// </summary>
    public string? TotalTime { get; init; }

    /// <summary>
    /// Estimated cost.
    /// </summary>
    public HowToCost? EstimatedCost { get; init; }

    /// <summary>
    /// Required supplies/materials.
    /// </summary>
    public IReadOnlyList<HowToSupply> Supplies { get; init; } = [];

    /// <summary>
    /// Required tools.
    /// </summary>
    public IReadOnlyList<HowToTool> Tools { get; init; } = [];

    /// <summary>
    /// Step-by-step instructions.
    /// </summary>
    public IReadOnlyList<HowToStep> Steps { get; init; } = [];

    /// <summary>
    /// Generates JSON-LD representation.
    /// </summary>
    public string ToJsonLd()
    {
        var schema = new Dictionary<string, object>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "HowTo",
            ["name"] = Name
        };

        if (Description is not null)
            schema["description"] = Description;
        if (TotalTime is not null)
            schema["totalTime"] = TotalTime;
        if (EstimatedCost is not null)
            schema["estimatedCost"] = new Dictionary<string, object>
            {
                ["@type"] = "MonetaryAmount",
                ["currency"] = EstimatedCost.Currency,
                ["value"] = EstimatedCost.Value
            };

        schema["supply"] = Supplies.Select(s => new Dictionary<string, object>
        {
            ["@type"] = "HowToSupply",
            ["name"] = s.Name
        }).ToArray();

        schema["tool"] = Tools.Select(t => new Dictionary<string, object>
        {
            ["@type"] = "HowToTool",
            ["name"] = t.Name
        }).ToArray();

        schema["step"] = Steps.Select((s, i) =>
        {
            var step = new Dictionary<string, object>
            {
                ["@type"] = "HowToStep",
                ["position"] = i + 1,
                ["name"] = s.Name,
                ["text"] = s.Text
            };
            if (s.ImageUrl is not null)
                step["image"] = s.ImageUrl;
            return step;
        }).ToArray();

        return JsonSerializer.Serialize(schema, JsonLdDefaults.IndentedOptions);
    }
}

/// <summary>
/// Cost estimate for HowTo.
/// </summary>
public record HowToCost
{
    public required string Currency { get; init; }
    public decimal Value { get; init; }
}

/// <summary>
/// A supply/material for HowTo.
/// </summary>
public record HowToSupply
{
    public required string Name { get; init; }
    public string? Url { get; init; }
}

/// <summary>
/// A tool for HowTo.
/// </summary>
public record HowToTool
{
    public required string Name { get; init; }
    public string? Url { get; init; }
}

/// <summary>
/// A step in HowTo instructions.
/// </summary>
public record HowToStep
{
    public required string Name { get; init; }
    public required string Text { get; init; }
    public string? ImageUrl { get; init; }
    public string? VideoUrl { get; init; }
    public IReadOnlyList<HowToDirection>? Directions { get; init; }
    public IReadOnlyList<HowToTip>? Tips { get; init; }
}

/// <summary>
/// A direction within a HowTo step.
/// </summary>
public record HowToDirection
{
    public required string Text { get; init; }
}

/// <summary>
/// A tip within a HowTo step.
/// </summary>
public record HowToTip
{
    public required string Text { get; init; }
}
