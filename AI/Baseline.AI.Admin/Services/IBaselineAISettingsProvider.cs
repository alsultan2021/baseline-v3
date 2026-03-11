using Baseline.AI;

namespace Baseline.AI.Admin.Services;

/// <summary>
/// Service for retrieving Baseline AI settings from the admin database.
/// </summary>
public interface IBaselineAISettingsProvider
{
    /// <summary>
    /// Gets the current AI settings, merging database values with appsettings.json defaults.
    /// </summary>
    /// <returns>The merged <see cref="BaselineAIOptions"/>.</returns>
    BaselineAIOptions GetSettings();

    /// <summary>
    /// Gets the current AI settings asynchronously.
    /// </summary>
    /// <returns>The merged <see cref="BaselineAIOptions"/>.</returns>
    Task<BaselineAIOptions> GetSettingsAsync();

    /// <summary>
    /// Refreshes cached settings from the database.
    /// </summary>
    void RefreshCache();
}
