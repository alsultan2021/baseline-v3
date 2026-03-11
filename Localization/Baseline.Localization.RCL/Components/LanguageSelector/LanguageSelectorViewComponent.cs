using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Websites;
using CMS.Websites.Routing;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Globalization;

namespace Baseline.Localization.Components;

/// <summary>
/// Renders a language selector/switcher.
/// </summary>
public class LanguageSelectorViewComponent(
    IInfoProvider<ContentLanguageInfo> contentLanguageInfoProvider,
    IPreferredLanguageRetriever preferredLanguageRetriever,
    IWebPageUrlRetriever webPageUrlRetriever,
    IWebPageDataContextRetriever webPageDataContextRetriever,
    IWebsiteChannelContext websiteChannelContext,
    IHttpContextAccessor httpContextAccessor,
    IMemoryCache memoryCache) : ViewComponent
{
    private static readonly TimeSpan LanguageCacheDuration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan ChannelCacheDuration = TimeSpan.FromMinutes(30);
    /// <summary>
    /// Renders the language selector.
    /// </summary>
    /// <param name="variant">Display variant.</param>
    /// <param name="showFlags">Whether to show country flags.</param>
    /// <param name="showNativeNames">Whether to show native language names.</param>
    public async Task<IViewComponentResult> InvokeAsync(
        LanguageSelectorVariant variant = LanguageSelectorVariant.Dropdown,
        bool showFlags = true,
        bool showNativeNames = true)
    {
        var allLanguages = GetCachedLanguages();
        var currentLanguageCode = preferredLanguageRetriever.Get();
        var httpContext = httpContextAccessor.HttpContext;

        // Try IWebPageDataContextRetriever first (content-tree pages),
        // then fall back to HttpContext.Items for custom scenarios
        int? webPageItemId = null;
        if (webPageDataContextRetriever.TryRetrieve(out var webPageData))
        {
            webPageItemId = webPageData.WebPage.WebPageItemID;
        }
        else if (httpContext?.Items["WebPageItemId"] is int itemId && itemId > 0)
        {
            webPageItemId = itemId;
        }

        // Determine primary language for URL prefix logic
        string? primaryLanguage = null;
        try
        {
            var channelLanguages = await GetChannelPrimaryLanguageAsync(allLanguages);
            primaryLanguage = channelLanguages;
        }
        catch
        {
            // Fall back - assume first language is primary
            primaryLanguage = allLanguages.FirstOrDefault()?.ContentLanguageName;
        }

        var currentPath = httpContext?.Request.Path.Value ?? "/";
        var currentQuery = httpContext?.Request.QueryString.Value ?? "";

        var languages = new List<LanguageInfo>();
        foreach (var lang in allLanguages)
        {
            // Try to get CultureInfo for native name
            CultureInfo? culture = null;
            try
            {
                culture = new CultureInfo(lang.ContentLanguageCultureFormat);
            }
            catch (CultureNotFoundException)
            {
                // Ignore - will use display name as fallback
            }

            // Resolve language-specific URL
            string url = string.Empty;

            // Strategy 1: Content-tree pages — use IWebPageUrlRetriever
            if (webPageItemId.HasValue)
            {
                try
                {
                    var pageUrl = await webPageUrlRetriever.Retrieve(webPageItemId.Value, lang.ContentLanguageName);
                    url = pageUrl?.RelativePath ?? string.Empty;
                }
                catch
                {
                    // URL not available for this language variant
                }
            }

            // Strategy 2: Non-content-tree pages — manipulate URL prefix
            if (string.IsNullOrEmpty(url))
            {
                url = BuildLanguagePrefixUrl(currentPath, currentQuery, lang.ContentLanguageName, primaryLanguage, currentLanguageCode);
            }

            languages.Add(new LanguageInfo
            {
                Code = lang.ContentLanguageName,
                Name = lang.ContentLanguageDisplayName,
                NativeName = culture?.NativeName ?? lang.ContentLanguageDisplayName,
                IsCurrent = string.Equals(lang.ContentLanguageName, currentLanguageCode, StringComparison.OrdinalIgnoreCase),
                Url = url
            });
        }

        var currentLanguage = languages.FirstOrDefault(l => l.IsCurrent);

        var model = new LanguageSelectorViewModel
        {
            Languages = languages,
            CurrentLanguage = currentLanguage,
            Variant = variant,
            ShowFlags = showFlags,
            ShowNativeNames = showNativeNames
        };

        return variant switch
        {
            LanguageSelectorVariant.List => View("List", model),
            LanguageSelectorVariant.Inline => View("Inline", model),
            _ => View(model)
        };
    }

    /// <summary>
    /// Builds a URL with the correct language prefix for non-content-tree pages.
    /// Primary language has no prefix, other languages get /{code}/path.
    /// </summary>
    private static string BuildLanguagePrefixUrl(
        string currentPath, string queryString,
        string targetLanguage, string? primaryLanguage, string currentLanguage)
    {
        bool isPrimary = string.Equals(targetLanguage, primaryLanguage, StringComparison.OrdinalIgnoreCase);
        bool currentIsPrimary = string.Equals(currentLanguage, primaryLanguage, StringComparison.OrdinalIgnoreCase);

        // Strip existing language prefix from path
        string pathWithoutLang = currentPath;
        if (!currentIsPrimary && currentPath.Length > 1)
        {
            var prefix = $"/{currentLanguage}";
            if (currentPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                pathWithoutLang = currentPath[prefix.Length..];
                if (string.IsNullOrEmpty(pathWithoutLang))
                {
                    pathWithoutLang = "/";
                }
            }
        }

        // Build new URL with target language prefix
        if (isPrimary)
        {
            return $"{pathWithoutLang}{queryString}";
        }

        return $"/{targetLanguage}{pathWithoutLang}{queryString}";
    }

    /// <summary>
    /// Gets the primary language code for the current website channel.
    /// </summary>
    private async Task<string?> GetChannelPrimaryLanguageAsync(List<ContentLanguageInfo> allLanguages)
    {
        var channelId = websiteChannelContext.WebsiteChannelID;
        if (channelId <= 0)
        {
            return null;
        }

        // Cache the website channel lookup - rarely changes
        var cacheKey = $"WebsiteChannel_{channelId}";
        if (!memoryCache.TryGetValue(cacheKey, out WebsiteChannelInfo? websiteChannel))
        {
            websiteChannel = (await new ObjectQuery<WebsiteChannelInfo>()
                .WhereEquals(nameof(WebsiteChannelInfo.WebsiteChannelID), channelId)
                .TopN(1)
                .GetEnumerableTypedResultAsync())
                .FirstOrDefault();

            if (websiteChannel is not null)
            {
                memoryCache.Set(cacheKey, websiteChannel, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = ChannelCacheDuration
                });
            }
        }

        if (websiteChannel is null)
        {
            return null;
        }

        var primaryLangId = websiteChannel.WebsiteChannelPrimaryContentLanguageID;
        var primaryLang = allLanguages.FirstOrDefault(l => l.ContentLanguageID == primaryLangId);
        return primaryLang?.ContentLanguageName;
    }

    /// <summary>
    /// Gets all content languages from cache or database.
    /// </summary>
    private List<ContentLanguageInfo> GetCachedLanguages()
    {
        const string cacheKey = "ContentLanguages_All";

        if (memoryCache.TryGetValue(cacheKey, out List<ContentLanguageInfo>? cached) && cached is not null)
        {
            return cached;
        }

        var languages = contentLanguageInfoProvider.Get().ToList();
        memoryCache.Set(cacheKey, languages, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = LanguageCacheDuration
        });
        return languages;
    }
}

/// <summary>
/// View model for language selector.
/// </summary>
public class LanguageSelectorViewModel
{
    public IReadOnlyList<LanguageInfo> Languages { get; set; } = [];
    public LanguageInfo? CurrentLanguage { get; set; }
    public LanguageSelectorVariant Variant { get; set; }
    public bool ShowFlags { get; set; } = true;
    public bool ShowNativeNames { get; set; } = true;
}

/// <summary>
/// Represents a language option.
/// </summary>
public class LanguageInfo
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string NativeName { get; set; } = string.Empty;
    public string? FlagUrl { get; set; }
    public string Url { get; set; } = string.Empty;
    public bool IsCurrent { get; set; }

    // Aliases for v2 compatibility
    public string CodeName => Code;
    public string Culture => Code;
}

/// <summary>
/// Language selector display variants.
/// </summary>
public enum LanguageSelectorVariant
{
    Dropdown,
    List,
    Inline
}
