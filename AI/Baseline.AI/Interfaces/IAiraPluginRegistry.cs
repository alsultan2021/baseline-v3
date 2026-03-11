namespace Baseline.AI;

/// <summary>
/// Provides read-only access to registered AIRA plugins for admin introspection.
/// </summary>
public interface IAiraPluginRegistry
{
    /// <summary>
    /// All registered <see cref="IAiraPlugin"/> instances.
    /// </summary>
    IReadOnlyList<IAiraPlugin> Plugins { get; }

    /// <summary>
    /// Gets the per-plugin options for the given plugin name.
    /// Returns a default instance if no options were configured.
    /// </summary>
    AiraPluginOptions GetOptions(string pluginName);
}
