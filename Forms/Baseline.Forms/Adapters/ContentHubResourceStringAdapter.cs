using Baseline.Forms.Localization.Services;
using Baseline.Localization.Services;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace Baseline.Forms.Localization.Adapters;

/// <summary>
/// Bridges Forms' <see cref="IResourceStringProvider"/> to Baseline.Localization's
/// <see cref="IContentHubStringLocalizer"/> for Content Hub-backed translations.
/// </summary>
public sealed class ContentHubResourceStringAdapter(
    IContentHubStringLocalizer localizer,
    ILogger<ContentHubResourceStringAdapter> logger) : IResourceStringProvider
{
    /// <inheritdoc />
    public string? GetString(string key, string? cultureCode = null)
    {
        var result = WithCulture(cultureCode, () => localizer[key]);

        if (result.ResourceNotFound)
        {
            logger.LogDebug("Resource string not found for key {Key}", key);
            return null;
        }

        return result.Value;
    }

    /// <inheritdoc />
    public Task<string?> GetStringAsync(string key, string? cultureCode = null)
    {
        // ContentHubStringLocalizer reads from pre-loaded cache (sync-safe)
        return Task.FromResult(GetString(key, cultureCode));
    }

    /// <inheritdoc />
    public Task<IDictionary<string, string>> GetStringsByPrefixAsync(
        string prefix, string? cultureCode = null)
    {
        IDictionary<string, string> result = WithCulture(cultureCode, () =>
            localizer
                .GetAllStrings(includeParentCultures: true)
                .Where(s => s.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .ToDictionary(s => s.Name, s => s.Value, StringComparer.OrdinalIgnoreCase));

        return Task.FromResult(result);
    }

    /// <summary>
    /// Executes <paramref name="action"/> with <see cref="CultureInfo.CurrentUICulture"/> temporarily
    /// set to <paramref name="cultureCode"/> when it differs from the current culture.
    /// </summary>
    private static T WithCulture<T>(string? cultureCode, Func<T> action)
    {
        if (string.IsNullOrEmpty(cultureCode))
        {
            return action();
        }

        var requested = CultureInfo.GetCultureInfo(cultureCode);
        if (CultureInfo.CurrentUICulture.Equals(requested))
        {
            return action();
        }

        var original = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = requested;
        try
        {
            return action();
        }
        finally
        {
            CultureInfo.CurrentUICulture = original;
        }
    }
}
