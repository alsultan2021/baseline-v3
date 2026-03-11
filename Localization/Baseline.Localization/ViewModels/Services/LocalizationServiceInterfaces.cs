using Baseline.Localization;

namespace Localization.Services;

/// <summary>
/// ILocalizationService → v3 Baseline.Localization.ILocalizationService
/// Note: This does NOT inherit from CMS.Core.ILocalizationService to avoid ambiguity
/// </summary>
public interface ILocalizationService : Baseline.Localization.ILocalizationService { }

/// <summary>
/// ICultureService → v3 ICultureService
/// </summary>
public interface ICultureService : Baseline.Localization.ICultureService { }

/// <summary>
/// ILanguageService
/// </summary>
public interface ILanguageService
{
    Task<IEnumerable<LanguageInfo>> GetLanguagesAsync();
    Task<LanguageInfo?> GetCurrentLanguageAsync();
    Task<LanguageInfo?> GetDefaultLanguageAsync();
}

/// <summary>
/// LanguageInfo
/// </summary>
public record LanguageInfo(string CultureCode, string DisplayName, bool IsDefault);
