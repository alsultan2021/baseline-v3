using System.Text.Json.Serialization;

namespace Baseline.AI.Admin.Models;

/// <summary>
/// Lightweight content-type descriptor sent to the React form component.
/// </summary>
public sealed class AIKBContentType
{
    [JsonPropertyName("contentTypeName")]
    public string ContentTypeName { get; set; } = "";

    [JsonPropertyName("contentTypeDisplayName")]
    public string ContentTypeDisplayName { get; set; } = "";

    public AIKBContentType() { }

    public AIKBContentType(string name, string displayName)
    {
        ContentTypeName = name;
        ContentTypeDisplayName = displayName;
    }
}
