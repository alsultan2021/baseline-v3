namespace Baseline.AI;

/// <summary>
/// DI keyed-service keys used by the AIRA plugins infrastructure.
/// </summary>
public static class AiraPluginServiceKeys
{
    /// <summary>
    /// Key for the <c>IChatCompletionService</c> captured before
    /// the plugins library wraps it with plugin injection.
    /// </summary>
    public const string OriginalChat = "Baseline.AI.OriginalKenticoAiraChat";
}
