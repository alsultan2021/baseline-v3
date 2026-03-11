using System.Data;
using System.Text;
using System.Xml.Linq;
using Baseline.Core.Services;
using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Helpers;
using CMS.Base;
using CMS.Websites;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baseline.Navigation;

/// <summary>
/// Implementation of sitemap service with multi-language support.
/// Respects MetaData_NoIndex and MetaData_ShowInSitemap reusable schema fields on CMS_ContentItemCommonData.
/// </summary>
public class SitemapService(
    IContentRetriever contentRetriever,
    IHttpContextAccessor httpContextAccessor,
    IInfoProvider<ContentLanguageInfo> contentLanguageProvider,
    ICoreOptionsProvider coreOptionsProvider,
    IOptions<BaselineNavigationOptions> options,
    ILogger<SitemapService> logger,
    ISitemapCustomizationService? customizationService = null) : ISitemapService
{
    private readonly ICoreOptionsProvider _coreOptionsProvider = coreOptionsProvider;
    private readonly SitemapOptions _options = options.Value.Sitemap;

    private static readonly XNamespace SitemapNs = "http://www.sitemaps.org/schemas/sitemap/0.9";
    private static readonly XNamespace ImageNs = "http://www.google.com/schemas/sitemap-image/1.1";
    private static readonly XNamespace VideoNs = "http://www.google.com/schemas/sitemap-video/1.1";
    private static readonly XNamespace XhtmlNs = "http://www.w3.org/1999/xhtml";

    public async Task<string> GenerateSitemapAsync()
    {
        // When multiple languages exist, return a sitemap index pointing to per-language sitemaps
        var languages = await GetAvailableLanguagesAsync();
        var languageList = languages.ToList();
        if (languageList.Count > 1)
        {
            return await GenerateSitemapIndexAsync();
        }

        var urls = await GetSitemapUrlsAsync();
        return GenerateSitemapXml(urls);
    }

    public async Task<string> GenerateSitemapAsync(string languageCode)
    {
        var urls = await GetSitemapUrlsAsync(languageCode);
        return GenerateSitemapXml(urls);
    }

    private string GenerateSitemapXml(IEnumerable<SitemapUrl> urls)
    {
        var urlList = urls.ToList();
        var hasImages = urlList.Any(u => u.Images.Count > 0);
        var hasVideos = urlList.Any(u => u.Videos.Count > 0);
        var hasAlternates = urlList.Any(u => u.Alternates.Count > 0);

        var urlsetElement = new XElement(SitemapNs + "urlset");

        // Only add namespaces that are actually used
        if (hasImages)
        {
            urlsetElement.Add(new XAttribute(XNamespace.Xmlns + "image", ImageNs));
        }
        if (hasVideos)
        {
            urlsetElement.Add(new XAttribute(XNamespace.Xmlns + "video", VideoNs));
        }
        if (hasAlternates)
        {
            urlsetElement.Add(new XAttribute(XNamespace.Xmlns + "xhtml", XhtmlNs));
        }

        urlsetElement.Add(urlList.Select(CreateUrlElement));

        var document = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            urlsetElement);

        return document.ToString();
    }

    public async Task<string> GenerateSitemapIndexAsync()
    {
        var baseUrl = GetBaseUrl();
        var languages = await GetAvailableLanguagesAsync();

        var sitemapElements = new List<XElement>();

        // Add per-language sitemaps
        foreach (var language in languages)
        {
            sitemapElements.Add(
                new XElement(SitemapNs + "sitemap",
                    new XElement(SitemapNs + "loc", $"{baseUrl}/sitemap-{language}.xml"),
                    new XElement(SitemapNs + "lastmod", DateTimeOffset.UtcNow.ToString("yyyy-MM-dd"))));
        }

        var document = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement(SitemapNs + "sitemapindex", sitemapElements));

        return document.ToString();
    }

    public async Task<string> GenerateSitemapSectionAsync(string section, int page = 1)
    {
        // Check if section is a language code
        var languages = await GetAvailableLanguagesAsync();
        if (languages.Contains(section, StringComparer.OrdinalIgnoreCase))
        {
            return await GenerateSitemapAsync(section);
        }

        // Default to main sitemap
        return await GenerateSitemapAsync();
    }

    public async Task<IEnumerable<SitemapUrl>> GetSitemapUrlsAsync()
    {
        var urls = new List<SitemapUrl>();
        var baseUrl = GetBaseUrl().TrimEnd('/');
        var languages = await GetLanguagesWithCultureAsync();
        var defaultLanguage = languages.FirstOrDefault();

        // Add home page with configurable priority/frequency
        if (_options.IncludeHomePage)
        {
            var homeAlternates = languages
                .Select(lang => new SitemapAlternate
                {
                    Language = lang.CultureFormat,
                    Url = lang.Name == defaultLanguage.Name ? baseUrl : $"{baseUrl}/{lang.Name}"
                })
                .ToList();

            homeAlternates.Add(new SitemapAlternate
            {
                Language = "x-default",
                Url = baseUrl
            });

            urls.Add(new SitemapUrl
            {
                Location = baseUrl,
                LastModified = DateTimeOffset.UtcNow,
                ChangeFrequency = _options.HomePageChangeFrequency,
                Priority = _options.HomePagePriority,
                Alternates = homeAlternates
            });
        }

        // Get included content types from channel settings
        var includedContentTypes = await GetIncludedContentTypesAsync();

        // Query pages per language for correct hreflang alternate URLs
        var pageUrls = await GetPageUrlsAsync(languages, includedContentTypes);
        urls.AddRange(pageUrls);

        // Apply customization service if registered
        if (customizationService is not null)
        {
            var customNodes = await customizationService.GetCustomNodesAsync();
            urls.AddRange(customNodes);

            var filteredUrls = new List<SitemapUrl>();
            foreach (var url in urls)
            {
                if (await customizationService.ShouldIncludeUrlAsync(url))
                {
                    filteredUrls.Add(await customizationService.ModifyUrlAsync(url));
                }
            }

            return filteredUrls;
        }

        return urls;
    }

    private async Task<IEnumerable<string>> GetIncludedContentTypesAsync()
    {
        // First check channel settings from admin UI
        var channelSettings = await _coreOptionsProvider.GetChannelSettingsAsync();
        var uiContentTypes = channelSettings?.SitemapIncludedContentTypes ?? [];

        if (uiContentTypes.Any())
        {
            return uiContentTypes;
        }

        // Fall back to code-based options
        return _options.IncludedContentTypes;
    }

    private async Task<IEnumerable<SitemapUrl>> GetPageUrlsAsync(
        IReadOnlyList<(string Name, string CultureFormat)> languages,
        IEnumerable<string> includedContentTypes)
    {
        var urls = new List<SitemapUrl>();
        var baseUrl = GetBaseUrl().TrimEnd('/');
        var defaultLanguage = languages.FirstOrDefault();
        var contentTypesList = includedContentTypes.ToList();

        if (contentTypesList.Count == 0)
        {
            return urls;
        }

        // Pre-query content items excluded from sitemap via metadata fields
        var excludedContentItemIds = GetExcludedContentItemIds();

        foreach (var contentType in contentTypesList)
        {
            try
            {
                // Query each language separately for correct per-language URLs
                var pageUrlsByLanguage = new Dictionary<int, Dictionary<string, string>>();
                var pageCultureByLanguage = new Dictionary<int, Dictionary<string, string>>();
                var pageLastModified = new Dictionary<int, DateTimeOffset?>();

                foreach (var lang in languages)
                {
                    var pages = await contentRetriever.RetrievePagesOfContentTypes<IWebPageFieldsSource>(
                        [contentType],
                        new RetrievePagesOfContentTypesParameters { LanguageName = lang.Name },
                        additionalQueryConfiguration: _ => { },
                        cacheSettings: new RetrievalCacheSettings(
                            cacheItemNameSuffix: $"sitemap|{contentType}|{lang.Name}"));

                    foreach (var page in pages)
                    {
                        if (excludedContentItemIds.Contains(page.SystemFields.ContentItemID)) continue;

                        var pageUrl = page.GetUrl();
                        if (pageUrl is null) continue;
                        var relativePath = pageUrl.RelativePath?.TrimStart('~').TrimEnd('/');
                        if (string.IsNullOrEmpty(relativePath) || relativePath == "/") continue;

                        var id = page.SystemFields.WebPageItemID;

                        if (!pageUrlsByLanguage.TryGetValue(id, out var langPaths))
                        {
                            langPaths = [];
                            pageUrlsByLanguage[id] = langPaths;
                            pageCultureByLanguage[id] = [];
                        }

                        langPaths[lang.Name] = relativePath;
                        pageCultureByLanguage[id][lang.Name] = lang.CultureFormat;

                        // Track latest modified date across languages
                        var lastMod = page.SystemFields.ContentItemCommonDataLastPublishedWhen;
                        if (!pageLastModified.TryGetValue(id, out var existing) || lastMod > existing)
                        {
                            pageLastModified[id] = lastMod;
                        }
                    }
                }

                // Build sitemap entries with correct per-language alternates
                foreach (var (pageId, langPaths) in pageUrlsByLanguage)
                {
                    var defaultPath = langPaths.GetValueOrDefault(defaultLanguage.Name);
                    var primaryPath = defaultPath ?? langPaths.Values.First();
                    var fullUrl = $"{baseUrl}{primaryPath}";

                    var alternates = new List<SitemapAlternate>();
                    foreach (var (langName, path) in langPaths)
                    {
                        alternates.Add(new SitemapAlternate
                        {
                            Language = pageCultureByLanguage[pageId][langName],
                            Url = $"{baseUrl}{path}"
                        });
                    }

                    if (defaultPath is not null)
                    {
                        alternates.Add(new SitemapAlternate
                        {
                            Language = "x-default",
                            Url = fullUrl
                        });
                    }

                    urls.Add(new SitemapUrl
                    {
                        Location = fullUrl,
                        LastModified = pageLastModified.GetValueOrDefault(pageId),
                        ChangeFrequency = _options.DefaultChangeFrequency,
                        Priority = _options.DefaultPriority,
                        Alternates = alternates
                    });
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "SitemapService: error querying content type '{ContentType}'", contentType);
            }
        }

        return urls;
    }

    public async Task<IEnumerable<SitemapUrl>> GetSitemapUrlsAsync(string languageCode)
    {
        // Query pages specifically in the requested language
        var urls = new List<SitemapUrl>();
        var baseUrl = GetBaseUrl().TrimEnd('/');
        var includedContentTypes = await GetIncludedContentTypesAsync();
        var contentTypesList = includedContentTypes.ToList();

        // Add home page for this language
        if (_options.IncludeHomePage)
        {
            var languages = await GetAvailableLanguagesAsync();
            var defaultLanguage = languages.FirstOrDefault();
            var isDefault = string.Equals(languageCode, defaultLanguage, StringComparison.OrdinalIgnoreCase);
            var homeUrl = isDefault ? baseUrl : $"{baseUrl}/{languageCode}";

            urls.Add(new SitemapUrl
            {
                Location = homeUrl,
                LastModified = DateTimeOffset.UtcNow,
                ChangeFrequency = _options.HomePageChangeFrequency,
                Priority = _options.HomePagePriority
            });
        }

        if (contentTypesList.Count == 0)
        {
            return urls;
        }

        // Pre-query content items excluded from sitemap via metadata fields
        var excludedContentItemIds = GetExcludedContentItemIds();

        foreach (var contentType in contentTypesList)
        {
            try
            {
                var pages = await contentRetriever.RetrievePagesOfContentTypes<IWebPageFieldsSource>(
                    [contentType],
                    new RetrievePagesOfContentTypesParameters
                    {
                        LanguageName = languageCode
                    },
                    additionalQueryConfiguration: _ => { },
                    cacheSettings: new RetrievalCacheSettings(
                        cacheItemNameSuffix: $"sitemap|{contentType}|{languageCode}"));

                foreach (var page in pages)
                {
                    if (excludedContentItemIds.Contains(page.SystemFields.ContentItemID)) continue;

                    var pageUrl = page.GetUrl();
                    if (pageUrl is null) continue;
                    var relativePath = pageUrl.RelativePath?.TrimStart('~').TrimEnd('/');

                    // Skip home page (already added above) and empty paths
                    if (string.IsNullOrEmpty(relativePath) || relativePath == "/" || relativePath == $"/{languageCode}")
                    {
                        continue;
                    }

                    var lastModified = page.SystemFields.ContentItemCommonDataLastPublishedWhen;

                    urls.Add(new SitemapUrl
                    {
                        Location = $"{baseUrl}{relativePath}",
                        LastModified = lastModified,
                        ChangeFrequency = "weekly",
                        Priority = 0.8
                    });
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "SitemapService: error querying '{ContentType}' for language '{Language}'",
                    contentType, languageCode);
            }
        }

        // Apply customization service if registered
        if (customizationService is not null)
        {
            var customNodes = await customizationService.GetCustomNodesAsync();
            urls.AddRange(customNodes);

            var filteredUrls = new List<SitemapUrl>();
            foreach (var url in urls)
            {
                if (await customizationService.ShouldIncludeUrlAsync(url))
                {
                    filteredUrls.Add(await customizationService.ModifyUrlAsync(url));
                }
            }

            return filteredUrls;
        }

        return urls;
    }

    public async Task<IEnumerable<string>> GetAvailableLanguagesAsync()
    {
        var languages = await contentLanguageProvider.Get()
            .GetEnumerableTypedResultAsync();

        return languages.Select(l => l.ContentLanguageName);
    }

    /// <summary>
    /// Gets available languages with their BCP-47 culture format codes for hreflang.
    /// Normalizes casing to proper BCP-47 format (ll-RR: lowercase language, uppercase region).
    /// </summary>
    private async Task<IReadOnlyList<(string Name, string CultureFormat)>> GetLanguagesWithCultureAsync()
    {
        var languages = await contentLanguageProvider.Get()
            .GetEnumerableTypedResultAsync();

        return languages
            .Select(l => (l.ContentLanguageName, NormalizeBcp47(l.ContentLanguageCultureFormat)))
            .ToList();
    }

    /// <summary>
    /// Normalizes a culture code to proper BCP-47 format (ll-RR).
    /// Example: "Fr-CA" → "fr-CA", "EN-us" → "en-US"
    /// </summary>
    private static string NormalizeBcp47(string cultureCode)
    {
        if (string.IsNullOrEmpty(cultureCode))
        {
            return cultureCode;
        }

        var parts = cultureCode.Split('-');
        if (parts.Length == 2)
        {
            return $"{parts[0].ToLowerInvariant()}-{parts[1].ToUpperInvariant()}";
        }

        // Single part (just language code)
        return parts[0].ToLowerInvariant();
    }

    private XElement CreateUrlElement(SitemapUrl url)
    {
        var elements = new List<object>
        {
            new XElement(SitemapNs + "loc", url.Location)
        };

        if (_options.IncludeLastModified && url.LastModified.HasValue)
        {
            elements.Add(new XElement(SitemapNs + "lastmod",
                url.LastModified.Value.ToString("yyyy-MM-dd")));
        }

        if (_options.IncludeChangeFrequency && !string.IsNullOrEmpty(url.ChangeFrequency))
        {
            elements.Add(new XElement(SitemapNs + "changefreq", url.ChangeFrequency));
        }

        if (_options.IncludePriority && url.Priority.HasValue)
        {
            elements.Add(new XElement(SitemapNs + "priority",
                url.Priority.Value.ToString("0.0")));
        }

        // Add images
        if (_options.IncludeImages)
        {
            foreach (var image in url.Images)
            {
                var imageElements = new List<object>
                {
                    new XElement(ImageNs + "loc", image.Location)
                };

                if (!string.IsNullOrEmpty(image.Caption))
                {
                    imageElements.Add(new XElement(ImageNs + "caption", image.Caption));
                }

                if (!string.IsNullOrEmpty(image.Title))
                {
                    imageElements.Add(new XElement(ImageNs + "title", image.Title));
                }

                elements.Add(new XElement(ImageNs + "image", imageElements));
            }
        }

        // Add videos
        if (_options.IncludeVideos)
        {
            foreach (var video in url.Videos)
            {
                var videoElements = new List<object>
                {
                    new XElement(VideoNs + "thumbnail_loc", video.ThumbnailLocation),
                    new XElement(VideoNs + "title", video.Title),
                    new XElement(VideoNs + "description", video.Description),
                    new XElement(VideoNs + "content_loc", video.ContentLocation)
                };

                if (video.Duration.HasValue)
                {
                    videoElements.Add(new XElement(VideoNs + "duration", video.Duration.Value));
                }

                if (video.PublicationDate.HasValue)
                {
                    videoElements.Add(new XElement(VideoNs + "publication_date",
                        video.PublicationDate.Value.ToString("yyyy-MM-ddTHH:mm:sszzz")));
                }

                elements.Add(new XElement(VideoNs + "video", videoElements));
            }
        }

        // Add alternate language versions
        foreach (var alt in url.Alternates)
        {
            elements.Add(new XElement(XhtmlNs + "link",
                new XAttribute("rel", "alternate"),
                new XAttribute("hreflang", alt.Language),
                new XAttribute("href", alt.Url)));
        }

        return new XElement(SitemapNs + "url", elements);
    }

    /// <summary>
    /// Queries CMS_ContentItemCommonData for content items that should be excluded from the sitemap
    /// based on MetaData_NoIndex and MetaData_ShowInSitemap reusable schema fields.
    /// </summary>
    private HashSet<int> GetExcludedContentItemIds()
    {
        try
        {
            const string query = """
                SELECT DISTINCT cd.ContentItemCommonDataContentItemID
                FROM CMS_ContentItemCommonData cd
                WHERE cd.ContentItemCommonDataIsLatest = 1
                  AND (cd.MetaData_NoIndex = 1
                    OR cd.MetaData_ShowInSitemap = 0)
                """;

            System.Data.DataSet? ds;
            using (new CMSConnectionScope(true))
            {
                ds = ConnectionHelper.ExecuteQuery(query, null, QueryTypeEnum.SQLQuery);
            }
            if (ds?.Tables.Count > 0)
            {
                return ds.Tables[0].Rows.Cast<DataRow>()
                    .Select(r => (int)r["ContentItemCommonDataContentItemID"])
                    .ToHashSet();
            }

            return [];
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "SitemapService: could not query sitemap exclusion metadata, including all pages");
            return [];
        }
    }

    private string GetBaseUrl()
    {
        var request = httpContextAccessor.HttpContext?.Request;
        if (request is null)
        {
            return string.Empty;
        }

        return $"{request.Scheme}://{request.Host}";
    }
}
