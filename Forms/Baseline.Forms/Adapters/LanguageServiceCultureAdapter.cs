using Baseline.Forms.Localization.Services;
using Baseline.Localization.Services;

namespace Baseline.Forms.Localization.Adapters;

/// <summary>
/// Bridges Forms' <see cref="ICultureProvider"/> to Baseline.Localization's
/// <see cref="ILanguageService"/> for XbK-native culture detection.
/// </summary>
public sealed class LanguageServiceCultureAdapter(
    ILanguageService languageService) : ICultureProvider
{
    /// <inheritdoc />
    public string GetCurrentCulture() =>
        languageService.GetCurrentLanguage();

    /// <inheritdoc />
    public async Task<IEnumerable<string>> GetSupportedCulturesAsync()
    {
        var languages = await languageService.GetAllLanguagesAsync();
        return languages.Select(l => l.Code);
    }
}
