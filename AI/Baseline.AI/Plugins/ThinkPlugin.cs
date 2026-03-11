using System.ComponentModel;

using Baseline.AI;

using Microsoft.SemanticKernel;

namespace Baseline.AI.Plugins;

/// <summary>
/// AIRA plugin providing a scratchpad for step-by-step reasoning.
/// Mimics MCP's <c>think</c> capability — the AI calls this to articulate
/// intermediate thoughts that remain in context but are not shown to the user.
/// </summary>
[Description("Provides a scratchpad for step-by-step reasoning and planning.")]
public sealed class ThinkPlugin : IAiraPlugin
{
    /// <inheritdoc />
    public string PluginName => "Think";

    /// <summary>
    /// Records a thought or reasoning step. The AI uses this to
    /// think through complex problems step by step. The thought
    /// is returned verbatim so it stays in the conversation context.
    /// </summary>
    [KernelFunction("think")]
    [Description("Use this to think step-by-step through a complex problem. " +
                 "Write your reasoning, analysis, or plan as the thought parameter. " +
                 "The thought is returned as-is so it stays in context. " +
                 "Call multiple times for multi-step reasoning.")]
    public string Think(
        [Description("Your reasoning, analysis, or intermediate thought")] string thought)
    {
        return thought;
    }
}
