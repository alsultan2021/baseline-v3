using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Websites;
using CMS.Websites.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Baseline.TabbedPages;

/// <summary>
/// Default implementation of ITabSeoService.
/// </summary>
public class TabSeoService(
    ITabbedPageService tabbedPageService,
    ITabRenderingService tabRenderingService,
    IContentQueryExecutor contentQueryExecutor,
    IWebsiteChannelContext websiteChannelContext,
    IInfoProvider<WebsiteChannelInfo> websiteChannelProvider,
    IOptions<BaselineTabbedPagesOptions> options,
    ILogger<TabSeoService> logger) : ITabSeoService
{
    private readonly BaselineTabbedPagesOptions _options = options.Value;
    /// <inheritdoc/>
    public async Task<string> GetStructuredDataAsync(int pageId)
    {
        logger.LogDebug("Getting structured data for page {PageId}", pageId);

        var tabs = (await tabbedPageService.GetTabsAsync(pageId)).ToList();
        if (tabs.Count == 0)
        {
            return string.Empty;
        }

        var structuredData = new Dictionary<string, object>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "WebPage",
            ["mainEntity"] = new Dictionary<string, object>
            {
                ["@type"] = "ItemList",
                ["itemListElement"] = tabs.Select((tab, index) => new Dictionary<string, object>
                {
                    ["@type"] = "ListItem",
                    ["position"] = index + 1,
                    ["name"] = tab.Title,
                    ["url"] = tabRenderingService.GetTabUrl(pageId, tab.Slug),
                    ["description"] = tab.Description ?? string.Empty
                }).ToArray()
            }
        };

        return JsonSerializer.Serialize(structuredData, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<TabSitemapEntry>> GetSitemapEntriesAsync()
    {
        logger.LogDebug("Getting sitemap entries for all tabbed pages");

        var entries = new List<TabSitemapEntry>();

        // Query all tabbed pages (TabParent content type)
        var tabParentContentType = _options.TabParentContentTypeName;

        try
        {
            var builder = new ContentItemQueryBuilder()
                .ForContentType(tabParentContentType, query => query
                    .ForWebsite(websiteChannelContext.WebsiteChannelName));

            var queryOptions = new ContentQueryExecutionOptions
            {
                ForPreview = false,
                IncludeSecuredItems = false // Only include public content in sitemap
            };

            var tabbedPages = await contentQueryExecutor.GetResult(builder, container =>
            {
                return new
                {
                    PageId = container.GetValue<int>(nameof(WebPageFields.WebPageItemID)),
                    Title = container.ContentItemName,
                    LastModified = container.GetValue<DateTime>("ContentItemCommonDataModifiedWhen")
                };
            }, queryOptions);

            foreach (var page in tabbedPages)
            {
                var tabs = await tabbedPageService.GetTabsAsync(page.PageId);

                foreach (var tab in tabs.Where(t => t.IsVisible && t.IsEnabled))
                {
                    var relativeUrl = tabRenderingService.GetTabUrl(page.PageId, tab.Slug);
                    var absoluteUrl = EnsureAbsoluteUrl(relativeUrl);

                    entries.Add(new TabSitemapEntry
                    {
                        Url = absoluteUrl,
                        Title = tab.Title,
                        ParentPageTitle = page.Title,
                        LastModified = page.LastModified,
                        ChangeFrequency = "weekly",
                        Priority = tab.IsDefault ? 0.8 : 0.6
                    });
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to retrieve tabbed page sitemap entries");
        }

        return entries;
    }

    /// <inheritdoc/>
    public async Task<string?> GetCanonicalUrlAsync(int pageId, string? tabSlug = null)
    {
        logger.LogDebug("Getting canonical URL for page {PageId}, tab {TabSlug}", pageId, tabSlug);

        if (string.IsNullOrEmpty(tabSlug))
        {
            // Get default tab
            var defaultTab = await tabbedPageService.GetDefaultTabAsync(pageId);
            tabSlug = defaultTab?.Slug;
        }

        if (string.IsNullOrEmpty(tabSlug))
        {
            return null;
        }

        return EnsureAbsoluteUrl(tabRenderingService.GetTabUrl(pageId, tabSlug));
    }

    /// <summary>
    /// Ensures a URL is absolute by prepending the site base URL.
    /// Resolves from Seo.SiteBaseUrl config, falls back to channel domain.
    /// </summary>
    private string EnsureAbsoluteUrl(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out _))
        {
            return url;
        }

        var baseUrl = _options.Seo.SiteBaseUrl?.TrimEnd('/');

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            // Fallback: resolve from channel domain
            var channel = websiteChannelProvider.Get(websiteChannelContext.WebsiteChannelID);
            var domain = channel?.WebsiteChannelDomain;
            if (!string.IsNullOrWhiteSpace(domain))
            {
                baseUrl = domain.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                    ? domain.TrimEnd('/')
                    : $"https://{domain.TrimEnd('/')}";
            }
        }

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            logger.LogWarning("No SiteBaseUrl configured and no channel domain found; sitemap URLs will be relative");
            return url;
        }

        return url.StartsWith('/')
            ? $"{baseUrl}{url}"
            : $"{baseUrl}/{url}";
    }
}
