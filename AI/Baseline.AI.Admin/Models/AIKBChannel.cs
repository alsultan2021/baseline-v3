using System.Text.Json.Serialization;

namespace Baseline.AI.Admin.Models;

/// <summary>
/// Lightweight website-channel descriptor sent to the React form component.
/// </summary>
public sealed class AIKBChannel
{
    [JsonPropertyName("channelName")]
    public string ChannelName { get; set; } = "";

    [JsonPropertyName("channelDisplayName")]
    public string ChannelDisplayName { get; set; } = "";

    public AIKBChannel() { }

    public AIKBChannel(string name, string displayName)
    {
        ChannelName = name;
        ChannelDisplayName = displayName;
    }
}
