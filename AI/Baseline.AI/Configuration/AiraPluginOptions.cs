namespace Baseline.AI;

/// <summary>
/// Per-plugin configuration options for AIRA plugin extensions.
/// When <see cref="EnhancementPrompt"/> is set, the plugin's function results
/// are enhanced by prepending the prompt to the tool result text.
/// </summary>
public sealed class AiraPluginOptions
{
    /// <summary>
    /// Prompt prepended to plugin function results before they are returned to the LLM.
    /// When <c>null</c> or empty, no enhancement is applied.
    /// </summary>
    public string? EnhancementPrompt { get; set; }
}
