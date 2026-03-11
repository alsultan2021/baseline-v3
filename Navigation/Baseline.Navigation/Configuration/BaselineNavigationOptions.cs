namespace Baseline.Navigation;

/// <summary>
/// Configuration options for Baseline v3 Navigation module.
/// </summary>
public class BaselineNavigationOptions
{
    /// <summary>
    /// Enable breadcrumb generation.
    /// Default: true
    /// </summary>
    public bool EnableBreadcrumbs { get; set; } = true;

    /// <summary>
    /// Enable sitemap.xml generation.
    /// Default: true
    /// </summary>
    public bool EnableSitemap { get; set; } = true;

    /// <summary>
    /// Enable menu generation.
    /// Default: true
    /// </summary>
    public bool EnableMenus { get; set; } = true;

    /// <summary>
    /// Enable mega menu support.
    /// Default: true
    /// </summary>
    public bool EnableMegaMenus { get; set; } = true;

    /// <summary>
    /// Enable dynamic navigation providers.
    /// Default: true
    /// </summary>
    public bool EnableDynamicNavigation { get; set; } = true;

    /// <summary>
    /// Enable navigation caching service.
    /// Default: true
    /// </summary>
    public bool EnableNavigationCaching { get; set; } = true;

    /// <summary>
    /// Enable accessibility helper service.
    /// Default: true
    /// </summary>
    public bool EnableAccessibilityHelpers { get; set; } = true;

    /// <summary>
    /// Maximum depth for navigation tree.
    /// Default: 5
    /// </summary>
    public int MaxNavigationDepth { get; set; } = 5;

    /// <summary>
    /// Breadcrumb configuration options.
    /// </summary>
    public BreadcrumbOptions Breadcrumbs { get; set; } = new();

    /// <summary>
    /// Sitemap configuration options.
    /// </summary>
    public SitemapOptions Sitemap { get; set; } = new();

    /// <summary>
    /// Menu configuration options.
    /// </summary>
    public MenuOptions Menus { get; set; } = new();
}

/// <summary>
/// Breadcrumb configuration options.
/// </summary>
public class BreadcrumbOptions
{
    /// <summary>
    /// Include the home page in breadcrumbs.
    /// Default: true
    /// </summary>
    public bool IncludeHome { get; set; } = true;

    /// <summary>
    /// Label for the home page breadcrumb.
    /// Default: "Home"
    /// </summary>
    public string HomeLabel { get; set; } = "Home";

    /// <summary>
    /// Include the current page in breadcrumbs.
    /// Default: true
    /// </summary>
    public bool IncludeCurrentPage { get; set; } = true;

    /// <summary>
    /// Make the current page a link.
    /// Default: false
    /// </summary>
    public bool CurrentPageIsLink { get; set; } = false;

    /// <summary>
    /// Maximum number of breadcrumb items.
    /// Default: 10
    /// </summary>
    public int MaxItems { get; set; } = 10;

    /// <summary>
    /// Generate JSON-LD structured data for breadcrumbs.
    /// Default: true
    /// </summary>
    public bool GenerateStructuredData { get; set; } = true;
}

/// <summary>
/// Sitemap configuration options.
/// </summary>
public class SitemapOptions
{
    /// <summary>
    /// Maximum number of URLs per sitemap file.
    /// Default: 50000 (per XML sitemap spec)
    /// </summary>
    public int MaxUrlsPerSitemap { get; set; } = 50000;

    /// <summary>
    /// Include images in sitemap.
    /// Default: true
    /// </summary>
    public bool IncludeImages { get; set; } = true;

    /// <summary>
    /// Include videos in sitemap.
    /// Default: false (enable via channel settings or code).
    /// </summary>
    public bool IncludeVideos { get; set; } = false;

    /// <summary>
    /// Include last modified date.
    /// Default: true
    /// </summary>
    public bool IncludeLastModified { get; set; } = true;

    /// <summary>
    /// Include change frequency.
    /// Default: true
    /// </summary>
    public bool IncludeChangeFrequency { get; set; } = true;

    /// <summary>
    /// Include priority.
    /// Default: true
    /// </summary>
    public bool IncludePriority { get; set; } = true;

    /// <summary>
    /// Default change frequency for pages.
    /// Default: "weekly"
    /// </summary>
    public string DefaultChangeFrequency { get; set; } = "weekly";

    /// <summary>
    /// Default priority for pages (0.0 - 1.0).
    /// Default: 0.5
    /// </summary>
    public double DefaultPriority { get; set; } = 0.5;

    /// <summary>
    /// Enable sitemap index for large sites.
    /// Default: true
    /// </summary>
    public bool EnableSitemapIndex { get; set; } = true;

    /// <summary>
    /// Cache duration for sitemap in minutes.
    /// Default: 60
    /// </summary>
    public int CacheDurationMinutes { get; set; } = 60;

    /// <summary>
    /// Content type class names to include in sitemap.
    /// When empty, all page content types are included.
    /// </summary>
    public IEnumerable<string> IncludedContentTypes { get; set; } = [];

    /// <summary>
    /// Include the home page (root URL) in the sitemap.
    /// Default: true
    /// </summary>
    public bool IncludeHomePage { get; set; } = true;

    /// <summary>
    /// Priority for the home page in the sitemap (0.0 - 1.0).
    /// Default: 1.0
    /// </summary>
    public double HomePagePriority { get; set; } = 1.0;

    /// <summary>
    /// Change frequency for the home page in the sitemap.
    /// Default: "daily"
    /// </summary>
    public string HomePageChangeFrequency { get; set; } = "daily";
}

/// <summary>
/// Menu configuration options.
/// </summary>
public class MenuOptions
{
    /// <summary>
    /// Cache menu structures.
    /// Default: true
    /// </summary>
    public bool EnableCaching { get; set; } = true;

    /// <summary>
    /// Cache duration in minutes.
    /// Default: 30
    /// </summary>
    public int CacheDurationMinutes { get; set; } = 30;

    /// <summary>
    /// Include hidden pages in admin preview.
    /// Default: true
    /// </summary>
    public bool IncludeHiddenInPreview { get; set; } = true;

    /// <summary>
    /// Automatically highlight active menu items.
    /// Default: true
    /// </summary>
    public bool AutoHighlightActive { get; set; } = true;

    /// <summary>
    /// Maximum depth for menu generation.
    /// Default: 3
    /// </summary>
    public int MaxDepth { get; set; } = 3;
}
