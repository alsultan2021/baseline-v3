namespace Baseline.TabbedPages;

/// <summary>
/// Represents a tab item.
/// </summary>
public class TabItem
{
    /// <summary>
    /// Tab ID.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Parent page ID.
    /// </summary>
    public int PageId { get; set; }

    /// <summary>
    /// Tab title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// URL-friendly slug.
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Tab order.
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Icon identifier or class.
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Short description for accessibility.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether this is the default tab.
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Whether the tab is visible.
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// Whether the tab is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// CSS class for this tab.
    /// </summary>
    public string? CssClass { get; set; }

    /// <summary>
    /// Custom data attributes.
    /// </summary>
    public Dictionary<string, string> DataAttributes { get; set; } = [];
}

/// <summary>
/// Represents tab content.
/// </summary>
public class TabContent
{
    /// <summary>
    /// Tab ID.
    /// </summary>
    public int TabId { get; set; }

    /// <summary>
    /// HTML content.
    /// </summary>
    public string Html { get; set; } = string.Empty;

    /// <summary>
    /// Widget zone content (for Page Builder integration).
    /// </summary>
    public string? WidgetZoneContent { get; set; }

    /// <summary>
    /// Whether content is rendered via Page Builder.
    /// </summary>
    public bool UsesPageBuilder { get; set; }

    /// <summary>
    /// Content type for the tab.
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// Last modified date.
    /// </summary>
    public DateTimeOffset LastModified { get; set; }
}

/// <summary>
/// View model for tabbed page rendering.
/// </summary>
public class TabbedPageViewModel
{
    /// <summary>
    /// Page ID.
    /// </summary>
    public int PageId { get; set; }

    /// <summary>
    /// Page title.
    /// </summary>
    public string PageTitle { get; set; } = string.Empty;

    /// <summary>
    /// All tabs.
    /// </summary>
    public IEnumerable<TabItem> Tabs { get; set; } = [];

    /// <summary>
    /// Active tab.
    /// </summary>
    public TabItem? ActiveTab { get; set; }

    /// <summary>
    /// Active tab content.
    /// </summary>
    public TabContent? ActiveTabContent { get; set; }

    /// <summary>
    /// All tab contents (for SEO rendering).
    /// </summary>
    public Dictionary<int, TabContent> AllTabContents { get; set; } = [];

    /// <summary>
    /// Rendering options.
    /// </summary>
    public TabRenderingOptions RenderingOptions { get; set; } = new();

    /// <summary>
    /// SEO options.
    /// </summary>
    public TabSeoOptions SeoOptions { get; set; } = new();

    /// <summary>
    /// Whether lazy loading is enabled. When true, inactive tab content
    /// is loaded on demand via the <see cref="LazyLoadApiUrl"/> endpoint.
    /// </summary>
    public bool LazyLoadEnabled { get; set; }

    /// <summary>
    /// API URL for lazy loading tab content (e.g., "/api/baseline/tabs/{pageId}").
    /// Null when lazy loading is disabled.
    /// </summary>
    public string? LazyLoadApiUrl { get; set; }

    /// <summary>
    /// Whether currently in edit mode.
    /// </summary>
    public bool IsEditMode { get; set; }
}

/// <summary>
/// Sitemap entry for a tab.
/// </summary>
public class TabSitemapEntry
{
    /// <summary>
    /// Full URL.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Last modified date.
    /// </summary>
    public DateTimeOffset LastModified { get; set; }

    /// <summary>
    /// Change frequency.
    /// </summary>
    public string ChangeFrequency { get; set; } = "weekly";

    /// <summary>
    /// Priority (0.0 to 1.0).
    /// </summary>
    public double Priority { get; set; } = 0.5;

    /// <summary>
    /// Tab title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Parent page title.
    /// </summary>
    public string ParentPageTitle { get; set; } = string.Empty;
}
