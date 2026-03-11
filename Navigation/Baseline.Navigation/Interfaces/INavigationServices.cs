using Baseline.Core;

namespace Baseline.Navigation;

/// <summary>
/// Service for generating breadcrumb navigation.
/// </summary>
public interface IBreadcrumbService
{
    /// <summary>
    /// Gets breadcrumbs for the current page.
    /// </summary>
    Task<IEnumerable<BreadcrumbItem>> GetBreadcrumbsAsync();

    /// <summary>
    /// Gets breadcrumbs for a specific page by path.
    /// </summary>
    Task<IEnumerable<BreadcrumbItem>> GetBreadcrumbsForPathAsync(string path);

    /// <summary>
    /// Gets breadcrumbs for a specific content item.
    /// </summary>
    Task<IEnumerable<BreadcrumbItem>> GetBreadcrumbsForContentAsync(int contentItemId);
}

/// <summary>
/// Service for generating XML sitemaps with multi-language support.
/// Automatically generates hreflang alternate links for all available language variants.
/// </summary>
public interface ISitemapService
{
    /// <summary>
    /// Generates the sitemap XML for all languages.
    /// Includes hreflang alternate links for each URL.
    /// </summary>
    Task<string> GenerateSitemapAsync();

    /// <summary>
    /// Generates the sitemap XML for a specific language.
    /// </summary>
    /// <param name="languageCode">The language code (e.g., "en", "fr").</param>
    Task<string> GenerateSitemapAsync(string languageCode);

    /// <summary>
    /// Generates a sitemap index for large sites.
    /// Includes references to language-specific sitemaps if enabled.
    /// </summary>
    Task<string> GenerateSitemapIndexAsync();

    /// <summary>
    /// Generates a specific sitemap section.
    /// </summary>
    Task<string> GenerateSitemapSectionAsync(string section, int page = 1);

    /// <summary>
    /// Gets all sitemap URLs for all languages.
    /// Each URL includes alternate language links.
    /// </summary>
    Task<IEnumerable<SitemapUrl>> GetSitemapUrlsAsync();

    /// <summary>
    /// Gets sitemap URLs for a specific language.
    /// </summary>
    /// <param name="languageCode">The language code to retrieve URLs for.</param>
    Task<IEnumerable<SitemapUrl>> GetSitemapUrlsAsync(string languageCode);

    /// <summary>
    /// Gets all available languages for the current website channel.
    /// </summary>
    Task<IEnumerable<string>> GetAvailableLanguagesAsync();
}

/// <summary>
/// Service for generating navigation menus.
/// </summary>
public interface IMenuService
{
    /// <summary>
    /// Gets a menu by its code name.
    /// </summary>
    Task<Menu?> GetMenuAsync(string menuCodeName);

    /// <summary>
    /// Gets the main navigation menu.
    /// </summary>
    Task<Menu?> GetMainMenuAsync();

    /// <summary>
    /// Gets the footer navigation menu.
    /// </summary>
    Task<Menu?> GetFooterMenuAsync();

    /// <summary>
    /// Gets a navigation tree starting from a specific path.
    /// </summary>
    Task<IEnumerable<NavigationItem>> GetNavigationTreeAsync(
        string? rootPath = null,
        int maxDepth = 3);

    /// <summary>
    /// Gets child navigation items for a specific path.
    /// </summary>
    Task<IEnumerable<NavigationItem>> GetChildNavigationAsync(string parentPath);
}

/// <summary>
/// Service for page URL resolution.
/// </summary>
public interface IPageUrlService
{
    /// <summary>
    /// Gets the URL for a web page item by its WebPageItemID.
    /// Note: This expects a WebPageItemID, not a ContentItemID.
    /// </summary>
    Task<string?> GetUrlAsync(int webPageItemId);

    /// <summary>
    /// Gets the URL for a page by GUID.
    /// </summary>
    Task<string?> GetUrlAsync(Guid contentItemGuid);

    /// <summary>
    /// Gets the absolute URL for a web page item.
    /// </summary>
    Task<string?> GetAbsoluteUrlAsync(int webPageItemId);

    /// <summary>
    /// Gets the canonical URL for the current page.
    /// </summary>
    Task<string?> GetCanonicalUrlAsync();
}

/// <summary>
/// Service for customizing sitemap generation.
/// </summary>
public interface ISitemapCustomizationService
{
    /// <summary>
    /// Gets custom sitemap nodes to add to the sitemap.
    /// </summary>
    Task<IEnumerable<SitemapUrl>> GetCustomNodesAsync();

    /// <summary>
    /// Filters sitemap URLs (return false to exclude).
    /// </summary>
    Task<bool> ShouldIncludeUrlAsync(SitemapUrl url);

    /// <summary>
    /// Modifies a sitemap URL before it's added.
    /// </summary>
    Task<SitemapUrl> ModifyUrlAsync(SitemapUrl url);
}

/// <summary>
/// Service for dynamic navigation items (not stored in CMS).
/// </summary>
public interface IDynamicNavigationService
{
    /// <summary>
    /// Gets dynamic navigation items for a menu.
    /// </summary>
    Task<IEnumerable<NavigationItem>> GetDynamicItemsAsync(string menuCodeName);

    /// <summary>
    /// Gets dynamic child items for a parent navigation item.
    /// </summary>
    Task<IEnumerable<NavigationItem>> GetDynamicChildItemsAsync(NavigationItem parent);

    /// <summary>
    /// Registers a dynamic navigation provider.
    /// </summary>
    void RegisterProvider(IDynamicNavigationProvider provider);
}

/// <summary>
/// Provider for dynamic navigation items.
/// </summary>
public interface IDynamicNavigationProvider
{
    /// <summary>
    /// Menu code names this provider handles.
    /// </summary>
    IEnumerable<string> SupportedMenus { get; }

    /// <summary>
    /// Gets dynamic navigation items.
    /// </summary>
    Task<IEnumerable<NavigationItem>> GetItemsAsync(string menuCodeName);

    /// <summary>
    /// Priority for ordering (higher = earlier).
    /// </summary>
    int Priority => 0;
}

/// <summary>
/// Interface for navigation item models used by tag helpers.
/// </summary>
public interface INavigationItemModel
{
    /// <summary>
    /// The navigation link href.
    /// </summary>
    string? Href { get; }

    /// <summary>
    /// The link title/tooltip.
    /// </summary>
    string? Title { get; }

    /// <summary>
    /// The link target (_blank, _self, etc.).
    /// </summary>
    string? Target { get; }

    /// <summary>
    /// CSS class(es) for the item.
    /// </summary>
    string? CssClass { get; }

    /// <summary>
    /// Whether this is the current/active page.
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Whether this is an external link.
    /// </summary>
    bool IsExternal { get; }
}
