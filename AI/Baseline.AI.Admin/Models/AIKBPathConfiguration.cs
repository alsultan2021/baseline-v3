using System.Text.Json.Serialization;

namespace Baseline.AI.Admin.Models;

/// <summary>
/// Represents a single KB path configuration — serialized to/from the React form component.
/// Mirrors the DB fields of <see cref="Data.AIKnowledgeBasePathInfo"/>.
/// </summary>
public sealed class AIKBPathConfiguration
{
    /// <summary>DB primary key. Null for newly-created paths.</summary>
    [JsonPropertyName("identifier")]
    public int? Identifier { get; set; }

    [JsonPropertyName("channelName")]
    public string ChannelName { get; set; } = "";

    [JsonPropertyName("channelDisplayName")]
    public string ChannelDisplayName { get; set; } = "";

    [JsonPropertyName("includePattern")]
    public string IncludePattern { get; set; } = "/%";

    [JsonPropertyName("excludePattern")]
    public string? ExcludePattern { get; set; }

    [JsonPropertyName("contentTypes")]
    public List<AIKBContentType> ContentTypes { get; set; } = [];

    [JsonPropertyName("priority")]
    public int Priority { get; set; }

    [JsonPropertyName("includeChildren")]
    public bool IncludeChildren { get; set; } = true;
}
