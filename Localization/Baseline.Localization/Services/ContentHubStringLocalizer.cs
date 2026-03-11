using CMS.ContentEngine;
using CMS.DataEngine;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace Baseline.Localization.Services;

/// <summary>
/// XbK Content Hub-based string localizer that retrieves translations from content items.
/// Implements ASP.NET Core IStringLocalizer pattern using XbK content as the source.
/// 
/// This approach allows editors to manage translations in the Content Hub without
/// needing developer intervention, following Kentico's recommended pattern.
/// </summary>
public interface IContentHubStringLocalizer : IStringLocalizer
{
    /// <summary>
    /// Gets a localized string by key, with fallback behavior.
    /// </summary>
    LocalizedString GetWithFallback(string key, string fallbackValue);

    /// <summary>
    /// Preloads all strings for the current language into cache.
    /// </summary>
    Task PreloadAsync();

    /// <summary>
    /// Clears the localization cache.
    /// </summary>
    void ClearCache();

    /// <summary>
    /// Gets missing translation keys for a language.
    /// </summary>
    Task<IEnumerable<string>> GetMissingTranslationsAsync(string languageCode);
}

/// <summary>
/// Configuration for content-based localization.
/// </summary>
public class ContentHubLocalizationOptions
{
    /// <summary>
    /// Content type code name for localization entries.
    /// Default: "Baseline.LocalizationString"
    /// </summary>
    public string ContentTypeCodeName { get; set; } = "Baseline.LocalizationString";

    /// <summary>
    /// Field name for the translation key.
    /// Default: "Key"
    /// </summary>
    public string KeyFieldName { get; set; } = "Key";

    /// <summary>
    /// Field name for the translation value.
    /// Default: "Value"
    /// </summary>
    public string ValueFieldName { get; set; } = "Value";

    /// <summary>
    /// Field name for the category/namespace.
    /// Default: "Category"
    /// </summary>
    public string CategoryFieldName { get; set; } = "Category";

    /// <summary>
    /// Cache duration in minutes.
    /// Default: 60
    /// </summary>
    public int CacheDurationMinutes { get; set; } = 60;

    /// <summary>
    /// Whether to use fallback language when translation is missing.
    /// Default: true
    /// </summary>
    public bool UseFallbackLanguage { get; set; } = true;

    /// <summary>
    /// Whether to return key as value when translation is missing.
    /// Default: true
    /// </summary>
    public bool ReturnKeyIfNotFound { get; set; } = true;

    /// <summary>
    /// Prefix to prepend to all keys.
    /// Default: null
    /// </summary>
    public string? KeyPrefix { get; set; }
}

/// <summary>
/// Content Hub-based string localizer implementation.
/// Queries localization content items from XbK Content Hub.
/// </summary>
public sealed class ContentHubStringLocalizer : IContentHubStringLocalizer, IDisposable
{
    private readonly IContentQueryExecutor _contentQueryExecutor;
    private readonly ILanguageService _languageService;
    private readonly ILogger<ContentHubStringLocalizer> _logger;
    private readonly ContentHubLocalizationOptions _options;
    private readonly string _cacheKeyPrefix = "Baseline.Localization.";
    private readonly HashSet<string> _trackedCacheKeys = [];
    private readonly object _cacheKeyLock = new();
    private MemoryCache _cache;

    public ContentHubStringLocalizer(
        IContentQueryExecutor contentQueryExecutor,
        ILanguageService languageService,
        ILogger<ContentHubStringLocalizer> logger,
        ContentHubLocalizationOptions options)
    {
        _contentQueryExecutor = contentQueryExecutor;
        _languageService = languageService;
        _logger = logger;
        _options = options;
        _cache = new MemoryCache(new MemoryCacheOptions());
    }

    public LocalizedString this[string name] => GetLocalizedString(name);

    public LocalizedString this[string name, params object[] arguments] =>
        GetLocalizedString(name, arguments);

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        var language = _languageService.GetCurrentLanguage();
        var strings = GetAllStringsFromCache(language);

        foreach (var (key, value) in strings)
        {
            yield return new LocalizedString(key, value, resourceNotFound: false);
        }

        if (includeParentCultures && _options.UseFallbackLanguage)
        {
            var fallbackChain = GetFallbackChainFromCache(language);
            foreach (var fallbackLang in fallbackChain)
            {
                var fallbackStrings = GetAllStringsFromCache(fallbackLang);
                foreach (var (key, value) in fallbackStrings)
                {
                    if (!strings.ContainsKey(key))
                    {
                        yield return new LocalizedString(key, value, resourceNotFound: false);
                    }
                }
            }
        }
    }

    public LocalizedString GetWithFallback(string key, string fallbackValue)
    {
        var result = GetLocalizedString(key);
        if (result.ResourceNotFound)
        {
            return new LocalizedString(key, fallbackValue, resourceNotFound: false);
        }
        return result;
    }

    public async Task PreloadAsync()
    {
        var language = _languageService.GetCurrentLanguage();
        await GetAllStringsForLanguageAsync(language);

        if (_options.UseFallbackLanguage)
        {
            var fallbackChain = await _languageService.GetFallbackChainAsync(language);

            // Cache the fallback chain itself
            var fallbackCacheKey = $"{_cacheKeyPrefix}fallback.{language}";
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_options.CacheDurationMinutes)
            };
            _cache.Set(fallbackCacheKey, fallbackChain.ToList(), cacheOptions);
            TrackCacheKey(fallbackCacheKey);

            foreach (var fallbackLang in fallbackChain)
            {
                await GetAllStringsForLanguageAsync(fallbackLang);
            }
        }

        _logger.LogInformation(
            "Preloaded localization strings for language '{Language}' and its fallback chain",
            language);
    }

    public void ClearCache()
    {
        lock (_cacheKeyLock)
        {
            _cache.Dispose();
            _cache = new MemoryCache(new MemoryCacheOptions());
            _trackedCacheKeys.Clear();
        }

        _logger.LogInformation("Localization cache cleared");
    }

    public async Task<IEnumerable<string>> GetMissingTranslationsAsync(string languageCode)
    {
        var defaultLanguage = await _languageService.GetDefaultLanguageAsync();
        var defaultStrings = await GetAllStringsForLanguageAsync(defaultLanguage);
        var targetStrings = await GetAllStringsForLanguageAsync(languageCode);

        return defaultStrings.Keys.Except(targetStrings.Keys);
    }

    private LocalizedString GetLocalizedString(string name, object[]? arguments = null)
    {
        var fullKey = _options.KeyPrefix is not null ? $"{_options.KeyPrefix}.{name}" : name;
        var language = _languageService.GetCurrentLanguage();

        // Try current language (cache only)
        var value = GetStringFromCache(fullKey, language);

        // Try fallback chain
        if (value is null && _options.UseFallbackLanguage)
        {
            var fallbackChain = GetFallbackChainFromCache(language);
            foreach (var fallbackLang in fallbackChain)
            {
                value = GetStringFromCache(fullKey, fallbackLang);
                if (value is not null)
                {
                    break;
                }
            }
        }

        if (value is null)
        {
            var notFoundValue = _options.ReturnKeyIfNotFound ? name : string.Empty;
            return new LocalizedString(name, notFoundValue, resourceNotFound: true);
        }

        // Apply format arguments
        if (arguments is { Length: > 0 })
        {
            value = string.Format(value, arguments);
        }

        return new LocalizedString(name, value, resourceNotFound: false);
    }

    private string? GetStringFromCache(string key, string language)
    {
        var allStrings = GetAllStringsFromCache(language);
        return allStrings.TryGetValue(key, out var value) ? value : null;
    }

    private Dictionary<string, string> GetAllStringsFromCache(string language)
    {
        var cacheKey = $"{_cacheKeyPrefix}all.{language}";

        if (_cache.TryGetValue<Dictionary<string, string>>(cacheKey, out var cached))
        {
            return cached ?? [];
        }

        _logger.LogWarning(
            "Localization strings for language '{Language}' not found in cache. " +
            "Ensure PreloadAsync() is called at application startup. Returning empty dictionary.",
            language);

        return [];
    }

    private List<string> GetFallbackChainFromCache(string language)
    {
        var cacheKey = $"{_cacheKeyPrefix}fallback.{language}";

        if (_cache.TryGetValue<List<string>>(cacheKey, out var cached))
        {
            return cached ?? [];
        }

        _logger.LogWarning(
            "Fallback chain for language '{Language}' not found in cache. " +
            "Ensure PreloadAsync() is called at application startup. Returning empty list.",
            language);

        return [];
    }

    private async Task<Dictionary<string, string>> GetAllStringsForLanguageAsync(string language)
    {
        var cacheKey = $"{_cacheKeyPrefix}all.{language}";

        if (_cache.TryGetValue<Dictionary<string, string>>(cacheKey, out var cached))
        {
            return cached ?? [];
        }

        try
        {
            // IContentQueryExecutor.GetResult is used here (instead of IContentRetriever)
            // because field names are configurable — requires IContentQueryDataContainer.GetValue<T>()
            var builder = new ContentItemQueryBuilder()
                .ForContentType(_options.ContentTypeCodeName)
                .InLanguage(language);

            // Use GetResult with IContentQueryDataContainer.GetValue<T>()
            // instead of reflection-based field access per Kentico docs
            var results = await _contentQueryExecutor.GetResult(
                builder,
                (IContentQueryDataContainer container) => new
                {
                    Key = container.GetValue<string>(_options.KeyFieldName),
                    Value = container.GetValue<string>(_options.ValueFieldName)
                },
                options: new ContentQueryExecutionOptions
                {
                    ForPreview = false,
                    IncludeSecuredItems = false
                });

            var strings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in results)
            {
                if (!string.IsNullOrEmpty(item.Key) && item.Value is not null)
                {
                    strings[item.Key] = item.Value;
                }
            }

            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_options.CacheDurationMinutes)
            };

            _cache.Set(cacheKey, strings, cacheOptions);
            TrackCacheKey(cacheKey);

            return strings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load localization strings for language {Language}", language);
            return [];
        }
    }

    private void TrackCacheKey(string key)
    {
        lock (_cacheKeyLock)
        {
            _trackedCacheKeys.Add(key);
        }
    }

    public void Dispose()
    {
        _cache.Dispose();
    }
}

/// <summary>
/// Factory for creating typed string localizers.
/// </summary>
public interface IContentHubStringLocalizerFactory : IStringLocalizerFactory
{
    /// <summary>
    /// Creates a localizer with a specific key prefix.
    /// </summary>
    IContentHubStringLocalizer CreateWithPrefix(string prefix);
}

/// <summary>
/// Factory implementation for content hub string localizers.
/// </summary>
public sealed class ContentHubStringLocalizerFactory : IContentHubStringLocalizerFactory
{
    private readonly IContentQueryExecutor _contentQueryExecutor;
    private readonly ILanguageService _languageService;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ContentHubLocalizationOptions _options;

    public ContentHubStringLocalizerFactory(
        IContentQueryExecutor contentQueryExecutor,
        ILanguageService languageService,
        ILoggerFactory loggerFactory,
        ContentHubLocalizationOptions options)
    {
        _contentQueryExecutor = contentQueryExecutor;
        _languageService = languageService;
        _loggerFactory = loggerFactory;
        _options = options;
    }

    public IStringLocalizer Create(Type resourceSource)
    {
        var prefix = resourceSource.FullName ?? resourceSource.Name;
        return CreateWithPrefix(prefix);
    }

    public IStringLocalizer Create(string baseName, string location)
    {
        var prefix = $"{location}.{baseName}";
        return CreateWithPrefix(prefix);
    }

    public IContentHubStringLocalizer CreateWithPrefix(string prefix)
    {
        var options = new ContentHubLocalizationOptions
        {
            ContentTypeCodeName = _options.ContentTypeCodeName,
            KeyFieldName = _options.KeyFieldName,
            ValueFieldName = _options.ValueFieldName,
            CategoryFieldName = _options.CategoryFieldName,
            CacheDurationMinutes = _options.CacheDurationMinutes,
            UseFallbackLanguage = _options.UseFallbackLanguage,
            ReturnKeyIfNotFound = _options.ReturnKeyIfNotFound,
            KeyPrefix = prefix
        };

        return new ContentHubStringLocalizer(
            _contentQueryExecutor,
            _languageService,
            _loggerFactory.CreateLogger<ContentHubStringLocalizer>(),
            options);
    }
}
