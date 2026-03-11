namespace Baseline.Forms.Localization.Services;

/// <summary>
/// Bridge to a localization module for reading resource strings.
/// Default implementation: <see cref="Baseline.Forms.Localization.Adapters.ContentHubResourceStringAdapter"/>.
/// </summary>
public interface IResourceStringProvider
{
    /// <summary>
    /// Gets a resource string.
    /// </summary>
    string? GetString(string key, string? cultureCode = null);

    /// <summary>
    /// Gets a resource string asynchronously.
    /// </summary>
    Task<string?> GetStringAsync(string key, string? cultureCode = null);

    /// <summary>
    /// Gets strings by prefix.
    /// </summary>
    Task<IDictionary<string, string>> GetStringsByPrefixAsync(string prefix, string? cultureCode = null);
}

/// <summary>
/// Bridge to a localization module for culture detection.
/// Default implementation: <see cref="Baseline.Forms.Localization.Adapters.LanguageServiceCultureAdapter"/>.
/// </summary>
public interface ICultureProvider
{
    /// <summary>
    /// Gets the current culture code.
    /// </summary>
    string GetCurrentCulture();

    /// <summary>
    /// Gets all supported culture codes.
    /// </summary>
    Task<IEnumerable<string>> GetSupportedCulturesAsync();
}
