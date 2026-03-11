using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Helpers;
using CMS.Websites.Routing;
using Kentico.Content.Web.Mvc.Routing;

namespace Baseline.Core;

/// <summary>
/// V3 implementation of language service.
/// </summary>
public class LanguageService(
    IPreferredLanguageRetriever preferredLanguageRetriever,
    IInfoProvider<ContentLanguageInfo> languageInfoProvider,
    IProgressiveCache progressiveCache,
    IWebsiteChannelContext websiteChannelContext) : ILanguageService
{
    // Common language to region mappings for OG locale
    private static readonly Dictionary<string, string> LanguageToRegion = new(StringComparer.OrdinalIgnoreCase)
    {
        { "en", "US" }, { "es", "ES" }, { "fr", "FR" }, { "de", "DE" },
        { "it", "IT" }, { "pt", "BR" }, { "zh", "CN" }, { "ja", "JP" },
        { "ko", "KR" }, { "ru", "RU" }, { "ar", "SA" }, { "nl", "NL" },
        { "pl", "PL" }, { "tr", "TR" }, { "cs", "CZ" }, { "sk", "SK" },
        { "hu", "HU" }, { "sv", "SE" }, { "da", "DK" }, { "no", "NO" }, { "fi", "FI" }
    };

    public LanguageInfo GetCurrentLanguage()
    {
        var cultureCode = preferredLanguageRetriever.Get();

        // Look up from cached languages to get accurate IsDefault/DisplayName
        var languages = GetAvailableLanguagesAsync().GetAwaiter().GetResult();
        var match = languages.FirstOrDefault(l =>
            l.CultureCode.Equals(cultureCode, StringComparison.OrdinalIgnoreCase));

        return match ?? new LanguageInfo
        {
            CultureCode = cultureCode,
            DisplayName = cultureCode,
            IsDefault = false
        };
    }

    public async Task<IEnumerable<LanguageInfo>> GetAvailableLanguagesAsync()
    {
        // Include channel name in cache key to support multi-channel setups
        var channelName = websiteChannelContext.WebsiteChannelName ?? "default";

        return await progressiveCache.LoadAsync(async cs =>
        {
            if (cs.Cached)
            {
                cs.CacheDependency = CacheHelper.GetCacheDependency($"{ContentLanguageInfo.OBJECT_TYPE}|all");
            }

            var languages = await languageInfoProvider
                .Get()
                .GetEnumerableTypedResultAsync();

            return languages.Select(lang => new LanguageInfo
            {
                CultureCode = lang.ContentLanguageName,
                DisplayName = lang.ContentLanguageDisplayName,
                IsDefault = lang.ContentLanguageIsDefault,
                NativeName = lang.ContentLanguageDisplayName
            }).OrderBy(l => l.DisplayName).ToList();
        }, new CacheSettings(60, $"v3_LanguageService_AvailableLanguages|{channelName}"));
    }

    public async Task<LanguageInfo?> GetLanguageAsync(string cultureCode)
    {
        var languages = await GetAvailableLanguagesAsync();
        return languages.FirstOrDefault(l =>
            l.CultureCode.Equals(cultureCode, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<LanguageInfo?> GetDefaultLanguageAsync()
    {
        var languages = await GetAvailableLanguagesAsync();
        return languages.FirstOrDefault(l => l.IsDefault);
    }
}
