using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baseline.TabbedPages;

/// <summary>
/// Default implementation of ITabRenderingService.
/// </summary>
public class TabRenderingService(
    ITabbedPageService tabbedPageService,
    IHttpContextAccessor httpContextAccessor,
    IOptions<BaselineTabbedPagesOptions> options,
    ILogger<TabRenderingService> logger) : ITabRenderingService
{
    private readonly BaselineTabbedPagesOptions _options = options.Value;

    /// <inheritdoc/>
    public async Task<TabbedPageViewModel> BuildViewModelAsync(int pageId, string? activeTabSlug = null)
    {
        logger.LogDebug("Building view model for page {PageId}, active tab: {ActiveTab}", pageId, activeTabSlug);

        var tabs = (await tabbedPageService.GetTabsAsync(pageId)).ToList();

        // Determine active tab
        TabItem? activeTab = null;
        if (!string.IsNullOrEmpty(activeTabSlug))
        {
            activeTab = tabs.FirstOrDefault(t =>
                string.Equals(t.Slug, activeTabSlug, StringComparison.OrdinalIgnoreCase));
        }
        activeTab ??= tabs.FirstOrDefault(t => t.IsDefault) ?? tabs.FirstOrDefault();

        var lazyLoad = _options.Behavior.LazyLoadContent;

        // Get active tab content (always loaded, even in lazy mode)
        TabContent? activeTabContent = null;
        if (activeTab != null)
        {
            activeTabContent = await tabbedPageService.GetTabContentAsync(activeTab.Id);
        }

        // Get all tab contents for SEO rendering (single batch query instead of N+1)
        // When lazy loading is enabled, skip batch loading — content loads on demand via API
        var allTabContents = new Dictionary<int, TabContent>();
        if (_options.Seo.RenderAllContentForSeo && !lazyLoad)
        {
            allTabContents = await tabbedPageService.GetTabContentsAsync(tabs.Select(t => t.Id));
        }

        return new TabbedPageViewModel
        {
            PageId = pageId,
            Tabs = tabs,
            ActiveTab = activeTab,
            ActiveTabContent = activeTabContent,
            AllTabContents = allTabContents,
            LazyLoadEnabled = lazyLoad,
            LazyLoadApiUrl = lazyLoad ? $"/api/baseline/tabs/{pageId}" : null,
            RenderingOptions = new TabRenderingOptions
            {
                TabStyle = _options.Rendering.TabStyle,
                EnableAnimation = _options.Rendering.EnableAnimation,
                VerticalTabs = _options.Rendering.VerticalTabs
            },
            SeoOptions = new TabSeoOptions
            {
                RenderAllContentForSeo = _options.Seo.RenderAllContentForSeo,
                AddStructuredData = _options.Seo.AddStructuredData
            }
        };
    }

    /// <inheritdoc/>
    public Task<string> RenderTabNavigationAsync(IEnumerable<TabItem> tabs, string? activeTabSlug = null)
    {
        var tabList = tabs.Where(t => t.IsVisible).OrderBy(t => t.Order).ToList();
        if (tabList.Count == 0)
        {
            return Task.FromResult(string.Empty);
        }

        var html = new System.Text.StringBuilder();
        html.Append("<ul class=\"nav nav-tabs\" role=\"tablist\">");

        foreach (var tab in tabList)
        {
            var isActive = activeTabSlug != null
                ? string.Equals(tab.Slug, activeTabSlug, StringComparison.OrdinalIgnoreCase)
                : tab.IsDefault;

            var cssClasses = new List<string> { "nav-link" };
            if (isActive) cssClasses.Add("active");
            if (!tab.IsEnabled) cssClasses.Add("disabled");
            if (!string.IsNullOrEmpty(tab.CssClass)) cssClasses.Add(Encode(tab.CssClass));

            var encodedSlug = Encode(tab.Slug);

            html.Append("<li class=\"nav-item\" role=\"presentation\">");
            html.Append($"<button class=\"{string.Join(" ", cssClasses)}\" ");
            html.Append($"id=\"tab-{encodedSlug}\" ");
            html.Append($"data-bs-toggle=\"tab\" ");
            html.Append($"data-bs-target=\"#panel-{encodedSlug}\" ");
            html.Append($"type=\"button\" role=\"tab\" ");
            html.Append($"aria-controls=\"panel-{encodedSlug}\" ");
            html.Append($"aria-selected=\"{(isActive ? "true" : "false")}\"");
            if (!tab.IsEnabled) html.Append(" disabled");

            // Add custom data attributes — encode both keys and values
            foreach (var attr in tab.DataAttributes)
            {
                html.Append($" data-{Encode(attr.Key)}=\"{Encode(attr.Value)}\"");
            }

            html.Append(">");

            if (!string.IsNullOrEmpty(tab.Icon))
            {
                html.Append($"<i class=\"{Encode(tab.Icon)}\"></i> ");
            }

            html.Append(Encode(tab.Title));
            html.Append("</button></li>");
        }

        html.Append("</ul>");
        return Task.FromResult(html.ToString());
    }

    /// <summary>
    /// HTML-encodes a string for safe attribute/content insertion.
    /// </summary>
    private static string Encode(string value) =>
        System.Net.WebUtility.HtmlEncode(value);

    /// <inheritdoc/>
    public string GetTabUrl(int pageId, string tabSlug)
    {
        var request = httpContextAccessor.HttpContext?.Request;
        if (request == null)
        {
            return $"?tab={tabSlug}";
        }

        var basePath = request.Path.Value ?? "/";

        if (_options.Behavior.PersistInUrl)
        {
            // Append tab slug to path
            return $"{basePath.TrimEnd('/')}/{tabSlug}";
        }
        else
        {
            // Use query string
            return $"{basePath}?tab={tabSlug}";
        }
    }

    /// <inheritdoc/>
    public string? ResolveActiveTabFromUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return null;
        }

        // Check query string first
        if (url.Contains("?tab="))
        {
            var queryIndex = url.IndexOf("?tab=", StringComparison.OrdinalIgnoreCase);
            var tabStart = queryIndex + 5;
            var tabEnd = url.IndexOf('&', tabStart);
            return tabEnd > 0 ? url[tabStart..tabEnd] : url[tabStart..];
        }

        // Otherwise check path
        var segments = url.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length > 0 ? segments[^1] : null;
    }
}
