using System.Collections.Concurrent;
using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Websites;
using CMS.Websites.Routing;
using Kentico.Content.Web.Mvc.Routing;

namespace Baseline.Localization.Services;

/// <summary>
/// XbK-native language service that integrates with IPreferredLanguageRetriever and ContentLanguageInfo.
/// Provides comprehensive language management for Xperience by Kentico.
/// </summary>
public interface ILanguageService
{
    /// <summary>
    /// Gets the current request's preferred language code.
    /// Uses XbK's IPreferredLanguageRetriever under the hood.
    /// </summary>
    string GetCurrentLanguage();

    /// <summary>
    /// Gets the primary language for the current website channel.
    /// </summary>
    Task<string> GetPrimaryLanguageAsync();

    /// <summary>
    /// Gets all languages configured in the system.
    /// </summary>
    Task<IEnumerable<LanguageInfo>> GetAllLanguagesAsync();

    /// <summary>
    /// Gets languages available for the current website channel.
    /// </summary>
    Task<IEnumerable<LanguageInfo>> GetChannelLanguagesAsync();

    /// <summary>
    /// Gets the fallback language for a given language.
    /// </summary>
    Task<string?> GetFallbackLanguageAsync(string languageCode);

    /// <summary>
    /// Gets the complete fallback chain for a language.
    /// </summary>
    Task<IEnumerable<string>> GetFallbackChainAsync(string languageCode);

    /// <summary>
    /// Checks if content exists in a specific language.
    /// </summary>
    Task<bool> ContentExistsInLanguageAsync(Guid contentItemGuid, string languageCode);

    /// <summary>
    /// Gets available language variants for a content item.
    /// </summary>
    Task<IEnumerable<LanguageVariantInfo>> GetContentLanguageVariantsAsync(Guid contentItemGuid);

    /// <summary>
    /// Gets the default language for the system.
    /// </summary>
    Task<string> GetDefaultLanguageAsync();

    /// <summary>
    /// Resolves a language code to its ContentLanguageInfo.
    /// </summary>
    Task<ContentLanguageInfo?> GetLanguageInfoAsync(string languageCode);
}

/// <summary>
/// Information about a language.
/// </summary>
public record LanguageInfo
{
    /// <summary>
    /// Language code name (e.g., "en", "fr", "de").
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Display name of the language.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Formatting culture code (e.g., "en-US").
    /// </summary>
    public required string FormattingCulture { get; init; }

    /// <summary>
    /// Flag icon identifier or URL.
    /// </summary>
    public string? FlagIcon { get; init; }

    /// <summary>
    /// Whether this is the default language.
    /// </summary>
    public bool IsDefault { get; init; }

    /// <summary>
    /// Fallback language code, if any.
    /// </summary>
    public string? FallbackLanguage { get; init; }

    /// <summary>
    /// Whether this is the primary language for the current channel.
    /// </summary>
    public bool IsPrimaryForChannel { get; init; }
}

/// <summary>
/// Information about a language variant of content.
/// </summary>
public record LanguageVariantInfo
{
    /// <summary>
    /// Language code.
    /// </summary>
    public required string LanguageCode { get; init; }

    /// <summary>
    /// Whether the variant exists (is translated).
    /// </summary>
    public bool Exists { get; init; }

    /// <summary>
    /// Whether the variant is published.
    /// </summary>
    public bool IsPublished { get; init; }

    /// <summary>
    /// URL for this language variant (if web page).
    /// </summary>
    public string? Url { get; init; }

    /// <summary>
    /// Last modified date of this variant.
    /// </summary>
    public DateTimeOffset? LastModified { get; init; }
}

/// <summary>
/// Default implementation of ILanguageService using XbK native APIs.
/// </summary>
public sealed class LanguageService(
    IPreferredLanguageRetriever preferredLanguageRetriever,
    IInfoProvider<ContentLanguageInfo> contentLanguageInfoProvider,
    IInfoProvider<WebsiteChannelInfo> websiteChannelInfoProvider,
    IContentQueryExecutor contentQueryExecutor,
    IWebsiteChannelContext websiteChannelContext) : ILanguageService
{
    private readonly ConcurrentDictionary<string, ContentLanguageInfo> _languageCache = new(StringComparer.OrdinalIgnoreCase);

    public string GetCurrentLanguage()
    {
        return preferredLanguageRetriever.Get();
    }

    public async Task<string> GetPrimaryLanguageAsync()
    {
        // Query WebsiteChannelInfo for the actual primary language per Kentico docs:
        // each website channel has a WebsiteChannelPrimaryContentLanguageID set at creation
        var channelId = websiteChannelContext.WebsiteChannelID;
        if (channelId > 0)
        {
            var channelResults = await websiteChannelInfoProvider.Get()
                .WhereEquals(nameof(WebsiteChannelInfo.WebsiteChannelID), channelId)
                .GetEnumerableTypedResultAsync();

            var channel = channelResults.FirstOrDefault();
            if (channel is not null && channel.WebsiteChannelPrimaryContentLanguageID > 0)
            {
                var langCode = await GetLanguageCodeByIdAsync(channel.WebsiteChannelPrimaryContentLanguageID);
                if (langCode is not null)
                {
                    return langCode;
                }
            }
        }

        // Fallback: use system default language
        return await GetDefaultLanguageAsync();
    }

    public async Task<IEnumerable<LanguageInfo>> GetAllLanguagesAsync()
    {
        var languages = (await contentLanguageInfoProvider.Get()
            .GetEnumerableTypedResultAsync()).ToList();

        var defaultLanguage = await GetDefaultLanguageAsync();
        var primaryLanguage = await GetPrimaryLanguageAsync();

        var result = new List<LanguageInfo>();
        foreach (var lang in languages)
        {
            string? fallback = lang.ContentLanguageFallbackContentLanguageID > 0
                ? await GetLanguageCodeByIdAsync(lang.ContentLanguageFallbackContentLanguageID)
                : null;

            result.Add(new LanguageInfo
            {
                Code = lang.ContentLanguageName,
                DisplayName = lang.ContentLanguageDisplayName,
                FormattingCulture = lang.ContentLanguageCultureFormat,
                IsDefault = lang.ContentLanguageIsDefault,
                FallbackLanguage = fallback,
                IsPrimaryForChannel = lang.ContentLanguageName.Equals(primaryLanguage, StringComparison.OrdinalIgnoreCase)
            });
        }
        return result;
    }

    public async Task<IEnumerable<LanguageInfo>> GetChannelLanguagesAsync()
    {
        // Per Kentico docs: "Languages are global and exist above website channels.
        // All website channels are automatically available in all defined languages."
        // There is no out-of-the-box way to restrict languages per channel.
        return await GetAllLanguagesAsync();
    }

    public async Task<string?> GetFallbackLanguageAsync(string languageCode)
    {
        var languageInfo = await GetLanguageInfoAsync(languageCode);
        if (languageInfo is null || languageInfo.ContentLanguageFallbackContentLanguageID <= 0)
        {
            return null;
        }

        return await GetLanguageCodeByIdAsync(languageInfo.ContentLanguageFallbackContentLanguageID);
    }

    public async Task<IEnumerable<string>> GetFallbackChainAsync(string languageCode)
    {
        var chain = new List<string>();
        var currentLanguage = languageCode;
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        while (!string.IsNullOrEmpty(currentLanguage) && !visited.Contains(currentLanguage))
        {
            visited.Add(currentLanguage);
            var fallback = await GetFallbackLanguageAsync(currentLanguage);

            if (!string.IsNullOrEmpty(fallback))
            {
                chain.Add(fallback);
                currentLanguage = fallback;
            }
            else
            {
                break;
            }
        }

        return chain;
    }

    public async Task<bool> ContentExistsInLanguageAsync(Guid contentItemGuid, string languageCode)
    {
        // IContentQueryExecutor is used here (instead of IContentRetriever)
        // because we query by GUID across any/unknown content type
        var builder = new ContentItemQueryBuilder()
            .ForContentType("", q => q.Where(w => w.WhereEquals("ContentItemGUID", contentItemGuid)))
            .InLanguage(languageCode, useLanguageFallbacks: false);

        var results = await contentQueryExecutor.GetMappedResult<IContentItemFieldsSource>(
            builder,
            options: new ContentQueryExecutionOptions { ForPreview = false, IncludeSecuredItems = false });

        return results.Any();
    }

    public async Task<IEnumerable<LanguageVariantInfo>> GetContentLanguageVariantsAsync(Guid contentItemGuid)
    {
        var allLanguages = await GetAllLanguagesAsync();
        var variants = new List<LanguageVariantInfo>();

        // Check each language by querying content directly (no circular dependency)
        // IContentQueryExecutor is used because content type is unknown
        foreach (var lang in allLanguages)
        {
            var builder = new ContentItemQueryBuilder()
                .ForContentType("", q => q.Where(w => w.WhereEquals("ContentItemGUID", contentItemGuid)))
                .InLanguage(lang.Code, useLanguageFallbacks: false);

            bool exists;
            try
            {
                var results = await contentQueryExecutor.GetMappedResult<IContentItemFieldsSource>(
                    builder,
                    options: new ContentQueryExecutionOptions { ForPreview = false, IncludeSecuredItems = false });
                exists = results.Any();
            }
            catch
            {
                exists = false;
            }

            variants.Add(new LanguageVariantInfo
            {
                LanguageCode = lang.Code,
                Exists = exists,
                IsPublished = exists, // Simplified - would need workflow state check
                LastModified = null // Would require IContentItemManager.GetContentItemLanguageMetadata
            });
        }

        return variants;
    }

    public async Task<string> GetDefaultLanguageAsync()
    {
        var results = await contentLanguageInfoProvider.Get()
            .WhereEquals(nameof(ContentLanguageInfo.ContentLanguageIsDefault), true)
            .GetEnumerableTypedResultAsync();

        var defaultLang = results.FirstOrDefault();
        return defaultLang?.ContentLanguageName ?? "en";
    }

    public async Task<ContentLanguageInfo?> GetLanguageInfoAsync(string languageCode)
    {
        if (_languageCache.TryGetValue(languageCode, out var cached))
        {
            return cached;
        }

        var results = await contentLanguageInfoProvider.Get()
            .WhereEquals(nameof(ContentLanguageInfo.ContentLanguageName), languageCode)
            .GetEnumerableTypedResultAsync();

        var languageInfo = results.FirstOrDefault();
        if (languageInfo is not null)
        {
            _languageCache[languageCode] = languageInfo;
        }

        return languageInfo;
    }

    private async Task<string?> GetLanguageCodeByIdAsync(int languageId)
    {
        var cached = _languageCache.Values.FirstOrDefault(l => l.ContentLanguageID == languageId);
        if (cached is not null)
        {
            return cached.ContentLanguageName;
        }

        var results = await contentLanguageInfoProvider.Get()
            .WhereEquals(nameof(ContentLanguageInfo.ContentLanguageID), languageId)
            .GetEnumerableTypedResultAsync();

        var lang = results.FirstOrDefault();
        if (lang is not null)
        {
            _languageCache[lang.ContentLanguageName] = lang;
            return lang.ContentLanguageName;
        }

        return null;
    }
}
