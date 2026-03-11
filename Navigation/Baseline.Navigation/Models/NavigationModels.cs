using Baseline.Core;

namespace Baseline.Navigation;

/// <summary>
/// Represents a navigation item in a menu or tree structure.
/// </summary>
public class NavigationItem : INavigationItemModel
{
    /// <summary>
    /// Display title for the navigation item.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// URL for the navigation item.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Optional CSS class for the navigation item.
    /// </summary>
    public string? CssClass { get; set; }

    /// <summary>
    /// Whether this item is the currently active page.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Whether this item is in the active path (ancestor of current page).
    /// </summary>
    public bool IsInActivePath { get; set; }

    /// <summary>
    /// Whether to open this link in a new window.
    /// Derived from Target property.
    /// </summary>
    public bool OpenInNewWindow => Target?.Equals("_blank", StringComparison.OrdinalIgnoreCase) == true;

    /// <summary>
    /// Link target attribute (e.g., "_blank", "_self").
    /// </summary>
    public string? Target { get; set; }

    /// <summary>
    /// OnClick JavaScript handler.
    /// </summary>
    public string? OnClick { get; set; }

    /// <summary>
    /// Whether this is a mega menu item.
    /// </summary>
    public bool IsMegaMenu { get; set; }

    /// <summary>
    /// Content tree path for this navigation item.
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    /// Whether this navigation generates dynamic child items.
    /// </summary>
    public bool IsDynamic { get; set; }

    /// <summary>
    /// Code name for dynamic navigation lookup.
    /// </summary>
    public string? DynamicCodeName { get; set; }

    /// <summary>
    /// Depth level in the navigation tree (0-based).
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// Child navigation items.
    /// </summary>
    public IList<NavigationItem> Children { get; set; } = [];

    /// <summary>
    /// Whether this item has children.
    /// </summary>
    public bool HasChildren => Children.Count > 0;

    /// <summary>
    /// Content item ID for this navigation item.
    /// </summary>
    public int? ContentItemId { get; set; }

    /// <summary>
    /// Optional icon class.
    /// </summary>
    public string? IconClass { get; set; }

    /// <summary>
    /// Optional description or subtitle.
    /// </summary>
    public string? Description { get; set; }

    // INavigationItemModel implementation
    string? INavigationItemModel.Href => Url;
    bool INavigationItemModel.IsExternal => Target?.Equals("_blank", StringComparison.OrdinalIgnoreCase) == true;
}

/// <summary>
/// Represents a complete navigation menu.
/// </summary>
public class Menu
{
    /// <summary>
    /// Code name of the menu.
    /// </summary>
    public string CodeName { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the menu.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Root navigation items.
    /// </summary>
    public IList<NavigationItem> Items { get; set; } = [];

    /// <summary>
    /// Whether the menu has any items.
    /// </summary>
    public bool HasItems => Items.Count > 0;
}

/// <summary>
/// Represents a URL in the sitemap.
/// </summary>
public class SitemapUrl
{
    /// <summary>
    /// The URL location.
    /// </summary>
    public required string Location { get; set; }

    /// <summary>
    /// Last modification date.
    /// </summary>
    public DateTimeOffset? LastModified { get; set; }

    /// <summary>
    /// Change frequency (always, hourly, daily, weekly, monthly, yearly, never).
    /// </summary>
    public string? ChangeFrequency { get; set; }

    /// <summary>
    /// Priority (0.0 to 1.0).
    /// </summary>
    public double? Priority { get; set; }

    /// <summary>
    /// Images associated with this URL.
    /// </summary>
    public IList<SitemapImage> Images { get; set; } = [];

    /// <summary>
    /// Alternate language versions.
    /// </summary>
    public IList<SitemapAlternate> Alternates { get; set; } = [];

    /// <summary>
    /// Videos associated with this URL.
    /// </summary>
    public IList<SitemapVideo> Videos { get; set; } = [];
}

/// <summary>
/// Represents a video in the sitemap per Google video sitemap spec.
/// See https://developers.google.com/search/docs/crawling-indexing/sitemaps/video-sitemaps
/// </summary>
public class SitemapVideo
{
    /// <summary>
    /// URL pointing to the actual video media file (content_loc).
    /// </summary>
    public required string ContentLocation { get; set; }

    /// <summary>
    /// URL to a thumbnail image (at least 160x90, max 1920x1080).
    /// </summary>
    public required string ThumbnailLocation { get; set; }

    /// <summary>
    /// Video title (max 100 characters recommended).
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// Video description (max 2048 characters).
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// Duration in seconds.
    /// </summary>
    public int? Duration { get; set; }

    /// <summary>
    /// Date the video was first published.
    /// </summary>
    public DateTimeOffset? PublicationDate { get; set; }
}

/// <summary>
/// Represents an image in the sitemap.
/// </summary>
public class SitemapImage
{
    /// <summary>
    /// Image URL.
    /// </summary>
    public required string Location { get; set; }

    /// <summary>
    /// Image caption.
    /// </summary>
    public string? Caption { get; set; }

    /// <summary>
    /// Image title.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Geographic location where the image was taken.
    /// </summary>
    public string? GeoLocation { get; set; }

    /// <summary>
    /// License URL.
    /// </summary>
    public string? License { get; set; }
}

/// <summary>
/// Represents an alternate language version in the sitemap.
/// </summary>
public class SitemapAlternate
{
    /// <summary>
    /// Language code (e.g., "en", "fr").
    /// </summary>
    public required string Language { get; set; }

    /// <summary>
    /// URL for this language version.
    /// </summary>
    public required string Url { get; set; }
}
