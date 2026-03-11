using System.Globalization;
using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Websites;
using CMS.Websites.Routing;
using Kentico.Content.Web.Mvc.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baseline.Localization.Services;

/// <summary>
/// Default implementation of ILocalizationService that delegates to ContentHubStringLocalizer.
/// </summary>
public sealed class LocalizationService(
    IContentHubStringLocalizer localizer,
    ILanguageService languageService) : ILocalizationService
{
    public string GetString(string key)
    {
        var result = localizer[key];
        return result.ResourceNotFound ? key : result.Value;
    }

    public string GetString(string key, params object[] args)
    {
        var result = localizer[key, args];
        return result.ResourceNotFound ? string.Format(key, args) : result.Value;
    }

    public string GetString(string key, string cultureCode)
    {
        // ContentHubStringLocalizer uses the current thread culture;
        // temporarily override to fetch for a specific culture
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo(cultureCode);
            var result = localizer[key];
            return result.ResourceNotFound ? key : result.Value;
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    public string GetString(string key, string cultureCode, params object[] args)
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo(cultureCode);
            var result = localizer[key, args];
            return result.ResourceNotFound ? string.Format(key, args) : result.Value;
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    public string? TryGetString(string key)
    {
        var result = localizer[key];
        return result.ResourceNotFound ? null : result.Value;
    }

    public async Task<IDictionary<string, string>> GetAllStringsAsync(string? cultureCode = null)
    {
        var lang = cultureCode ?? languageService.GetCurrentLanguage();
        var strings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var s in localizer.GetAllStrings(includeParentCultures: true))
        {
            strings[s.Name] = s.Value;
        }

        return strings;
    }

    public async Task<IDictionary<string, string>> GetStringsByPrefixAsync(
        string prefix, string? cultureCode = null)
    {
        var all = await GetAllStringsAsync(cultureCode);
        return all
            .Where(kv => kv.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);
    }
}

/// <summary>
/// Default implementation of ICultureService backed by BaselineLocalizationOptions.
/// </summary>
public sealed class CultureService(
    BaselineLocalizationOptions options,
    IPreferredLanguageRetriever preferredLanguageRetriever,
    IWebPageUrlRetriever webPageUrlRetriever,
    IHttpContextAccessor httpContextAccessor,
    ILogger<CultureService> logger) : ICultureService
{
    public BaselineCultureInfo CurrentCulture
    {
        get
        {
            var code = preferredLanguageRetriever.Get();
            return GetCulture(code) ?? new BaselineCultureInfo(code);
        }
    }

    public IEnumerable<BaselineCultureInfo> SupportedCultures => options.SupportedCultures;

    public BaselineCultureInfo DefaultCulture =>
        options.SupportedCultures
            .FirstOrDefault(c => c.Code.Equals(options.DefaultCulture, StringComparison.OrdinalIgnoreCase))
        ?? new BaselineCultureInfo(options.DefaultCulture);

    public void SetCulture(string cultureCode)
    {
        var culture = new CultureInfo(cultureCode);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }

    public BaselineCultureInfo? GetCulture(string cultureCode)
    {
        return options.SupportedCultures.FirstOrDefault(c =>
            c.Code.Equals(cultureCode, StringComparison.OrdinalIgnoreCase) ||
            c.ShortCode.Equals(cultureCode, StringComparison.OrdinalIgnoreCase));
    }

    public bool IsCultureSupported(string cultureCode)
    {
        return GetCulture(cultureCode) is not null;
    }

    public async Task<string?> GetCultureUrlAsync(string cultureCode)
    {
        try
        {
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext?.Items["WebPageItemId"] is int webPageItemId && webPageItemId > 0)
            {
                var url = await webPageUrlRetriever.Retrieve(webPageItemId, cultureCode);
                return url.RelativePath;
            }
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Could not resolve culture URL for {CultureCode}", cultureCode);
        }

        return null;
    }
}

/// <summary>
/// Default implementation of ILocalizedUrlService using XbK APIs.
/// </summary>
public sealed class LocalizedUrlService(
    IWebPageUrlRetriever webPageUrlRetriever,
    ILanguageService languageService,
    IHttpContextAccessor httpContextAccessor,
    BaselineLocalizationOptions options,
    ILogger<LocalizedUrlService> logger) : ILocalizedUrlService
{
    public async Task<string?> GetLocalizedUrlAsync(int contentItemId, string cultureCode)
    {
        try
        {
            var url = await webPageUrlRetriever.Retrieve(contentItemId, cultureCode);
            return url.RelativePath;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Failed to retrieve URL for item {Id} in {Culture}",
                contentItemId, cultureCode);
            return null;
        }
    }

    public async Task<IEnumerable<AlternateLanguageUrl>> GetAlternateUrlsAsync(int contentItemId)
    {
        var languages = await languageService.GetAllLanguagesAsync();
        var primaryLang = await languageService.GetPrimaryLanguageAsync();
        var results = new List<AlternateLanguageUrl>();

        foreach (var lang in languages)
        {
            try
            {
                var url = await webPageUrlRetriever.Retrieve(contentItemId, lang.Code);
                var isPrimary = lang.Code.Equals(primaryLang, StringComparison.OrdinalIgnoreCase);

                results.Add(new AlternateLanguageUrl
                {
                    CultureCode = lang.Code,
                    Hreflang = lang.FormattingCulture,
                    Url = url.RelativePath,
                    IsDefault = isPrimary
                });

                // Add x-default for primary language
                if (isPrimary)
                {
                    results.Add(new AlternateLanguageUrl
                    {
                        CultureCode = lang.Code,
                        Hreflang = "x-default",
                        Url = url.RelativePath,
                        IsDefault = true
                    });
                }
            }
            catch
            {
                // Language variant URL not available — skip
            }
        }

        return results;
    }

    public async Task<string?> GetCanonicalUrlAsync()
    {
        try
        {
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext?.Items["WebPageItemId"] is int pageId && pageId > 0)
            {
                var currentLang = languageService.GetCurrentLanguage();
                var url = await webPageUrlRetriever.Retrieve(pageId, currentLang);
                return url.RelativePath;
            }
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Failed to get canonical URL");
        }

        return null;
    }

    public string? ResolveCultureFromUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return null;
        }

        // Extract first path segment and check if it matches a supported culture
        var path = url.TrimStart('/');
        var firstSegment = path.Split('/').FirstOrDefault();

        if (string.IsNullOrEmpty(firstSegment))
        {
            return null;
        }

        var match = options.SupportedCultures.FirstOrDefault(c =>
            c.ShortCode.Equals(firstSegment, StringComparison.OrdinalIgnoreCase) ||
            c.Code.Equals(firstSegment, StringComparison.OrdinalIgnoreCase));

        return match?.Code;
    }

    public string BuildLocalizedUrl(string path, string cultureCode)
    {
        var isPrimary = cultureCode.Equals(
            options.DefaultCulture, StringComparison.OrdinalIgnoreCase);

        if (isPrimary && options.HideDefaultCultureInUrl)
        {
            return path.StartsWith('/') ? path : $"/{path}";
        }

        // Get the short code for URL prefix
        var culture = options.SupportedCultures.FirstOrDefault(c =>
            c.Code.Equals(cultureCode, StringComparison.OrdinalIgnoreCase));

        var prefix = culture?.ShortCode ?? cultureCode;
        var cleanPath = path.TrimStart('/');

        return $"/{prefix}/{cleanPath}";
    }
}

/// <summary>
/// Default implementation of IResourceStringService.
/// Uses ContentHubStringLocalizer for read operations and IContentQueryExecutor for CRUD.
/// Note: Write operations require the Baseline.LocalizationString content type in XbK.
/// </summary>
public sealed class ResourceStringService(
    IContentHubStringLocalizer localizer,
    ILanguageService languageService,
    ILogger<ResourceStringService> logger) : IResourceStringService
{
    public async Task<ResourceString?> GetAsync(string key)
    {
        var result = localizer[key];
        if (result.ResourceNotFound)
        {
            return null;
        }

        return new ResourceString
        {
            Key = key,
            DefaultValue = result.Value,
            Translations = new Dictionary<string, string>
            {
                [languageService.GetCurrentLanguage()] = result.Value
            },
            LastModified = DateTimeOffset.UtcNow
        };
    }

    public Task<ResourceStringResult> SaveAsync(ResourceString resourceString)
    {
        // Write operations require IContentItemManager (admin API)
        // Not implemented in Baseline — use XbK admin UI or Content Hub API
        logger.LogWarning(
            "ResourceStringService.SaveAsync is not implemented. " +
            "Use the XbK admin UI or Content Hub API to manage localization strings.");
        return Task.FromResult(ResourceStringResult.Failed(
            "Write operations require XbK admin API. Use Content Hub to manage strings."));
    }

    public Task<ResourceStringResult> DeleteAsync(string key)
    {
        logger.LogWarning(
            "ResourceStringService.DeleteAsync is not implemented. " +
            "Use the XbK admin UI to delete localization strings.");
        return Task.FromResult(ResourceStringResult.Failed(
            "Write operations require XbK admin API. Use Content Hub to manage strings."));
    }

    public async Task<PagedResult<ResourceString>> GetAllAsync(ResourceStringQuery query)
    {
        var allStrings = localizer.GetAllStrings(includeParentCultures: true).ToList();
        var currentLang = languageService.GetCurrentLanguage();

        IEnumerable<ResourceString> items = allStrings.Select(s => new ResourceString
        {
            Key = s.Name,
            DefaultValue = s.Value,
            Translations = new Dictionary<string, string> { [currentLang] = s.Value },
            Category = s.Name.Contains('.') ? s.Name[..s.Name.LastIndexOf('.')] : null
        });

        // Apply filters
        if (!string.IsNullOrEmpty(query.KeyPattern))
        {
            items = items.Where(i =>
                i.Key.Contains(query.KeyPattern, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrEmpty(query.Category))
        {
            items = items.Where(i =>
                i.Category?.Equals(query.Category, StringComparison.OrdinalIgnoreCase) == true);
        }

        var list = items.ToList();
        var total = list.Count;

        // Apply sorting
        list = query.SortDescending
            ? [.. list.OrderByDescending(i => i.Key)]
            : [.. list.OrderBy(i => i.Key)];

        // Apply pagination
        var paged = list
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        return new PagedResult<ResourceString>
        {
            Items = paged,
            TotalCount = total,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    public Task<ImportResult> ImportAsync(Stream fileStream, string format)
    {
        logger.LogWarning("ResourceStringService.ImportAsync is not implemented.");
        return Task.FromResult(new ImportResult
        {
            Success = false,
            Errors = ["Import requires XbK admin API. Use CI/CD or Content Hub to manage strings."]
        });
    }

    public Task<Stream> ExportAsync(string format, string? cultureCode = null)
    {
        logger.LogWarning("ResourceStringService.ExportAsync is not implemented.");
        return Task.FromResult<Stream>(Stream.Null);
    }

    public async Task<IEnumerable<string>> GetMissingTranslationsAsync(string cultureCode)
    {
        return await localizer.GetMissingTranslationsAsync(cultureCode);
    }
}
