namespace Baseline.TabbedPages;

/// <summary>
/// Service for managing tabbed page content.
/// </summary>
public interface ITabbedPageService
{
    /// <summary>
    /// Gets all tabs for a page.
    /// </summary>
    Task<IEnumerable<TabItem>> GetTabsAsync(int pageId);

    /// <summary>
    /// Gets a specific tab by ID.
    /// </summary>
    Task<TabItem?> GetTabAsync(int tabId);

    /// <summary>
    /// Gets a specific tab by slug.
    /// </summary>
    Task<TabItem?> GetTabBySlugAsync(int pageId, string slug);

    /// <summary>
    /// Gets the default tab for a page.
    /// </summary>
    Task<TabItem?> GetDefaultTabAsync(int pageId);

    /// <summary>
    /// Gets tab content with caching.
    /// </summary>
    Task<TabContent?> GetTabContentAsync(int tabId);

    /// <summary>
    /// Gets content for multiple tabs in a single batch query.
    /// </summary>
    Task<Dictionary<int, TabContent>> GetTabContentsAsync(IEnumerable<int> tabIds);
}

/// <summary>
/// Service for rendering tabbed page content.
/// </summary>
public interface ITabRenderingService
{
    /// <summary>
    /// Renders the tab container.
    /// </summary>
    Task<TabbedPageViewModel> BuildViewModelAsync(int pageId, string? activeTabSlug = null);

    /// <summary>
    /// Gets tab navigation HTML.
    /// </summary>
    Task<string> RenderTabNavigationAsync(IEnumerable<TabItem> tabs, string? activeTabSlug = null);

    /// <summary>
    /// Gets the URL for a specific tab.
    /// </summary>
    string GetTabUrl(int pageId, string tabSlug);

    /// <summary>
    /// Resolves the active tab from URL.
    /// </summary>
    string? ResolveActiveTabFromUrl(string url);
}

/// <summary>
/// Service for tab SEO.
/// </summary>
public interface ITabSeoService
{
    /// <summary>
    /// Gets structured data for tabbed content.
    /// </summary>
    Task<string> GetStructuredDataAsync(int pageId);

    /// <summary>
    /// Gets sitemap entries for tabs.
    /// </summary>
    Task<IEnumerable<TabSitemapEntry>> GetSitemapEntriesAsync();

    /// <summary>
    /// Gets canonical URL for a tab.
    /// </summary>
    Task<string?> GetCanonicalUrlAsync(int pageId, string? tabSlug = null);
}
