namespace Baseline.AI.Services;

/// <summary>
/// Default implementation of <see cref="IAiraPluginRegistry"/>.
/// Created by <c>UseAiraPlugins()</c> with fully resolved plugin instances and options.
/// </summary>
internal sealed class AiraPluginRegistry : IAiraPluginRegistry
{
    private static readonly AiraPluginOptions DefaultOptions = new();

    public IReadOnlyList<IAiraPlugin> Plugins { get; }

    /// <summary>
    /// Internal access to the per-plugin options dictionary.
    /// Used by <see cref="AiraPluginChatServiceBase"/> to create the enhancement filter.
    /// </summary>
    internal IReadOnlyDictionary<string, AiraPluginOptions> AllOptions { get; }

    /// <summary>
    /// Empty fallback used when no plugins are registered.
    /// </summary>
    public AiraPluginRegistry()
    {
        Plugins = Array.Empty<IAiraPlugin>();
        AllOptions = new Dictionary<string, AiraPluginOptions>();
    }

    private AiraPluginRegistry(
        IEnumerable<IAiraPlugin> plugins,
        Dictionary<string, AiraPluginOptions> optionsByPlugin)
    {
        Plugins = plugins.ToList().AsReadOnly();
        AllOptions = optionsByPlugin;
    }

    public AiraPluginOptions GetOptions(string pluginName)
    {
        if (string.IsNullOrEmpty(pluginName))
            return DefaultOptions;

        return AllOptions.TryGetValue(pluginName, out var options) ? options : DefaultOptions;
    }

    /// <summary>
    /// Factory for creating from DI with resolved instances.
    /// </summary>
    internal static AiraPluginRegistry Create(
        IEnumerable<IAiraPlugin> plugins,
        Dictionary<string, AiraPluginOptions> optionsByPlugin)
        => new(plugins, optionsByPlugin);
}
