namespace Baseline.Core;

/// <summary>
/// Service for consolidated SEO metadata across content types.
/// Scoped per HTTP request - stores and retrieves page metadata.
/// Replaces project-specific WebPageMetaService implementations.
/// Note: v3 uses BaselinePageMetaData to avoid conflict with v2 Core.Models.PageMetaData
/// </summary>
public interface IMetaDataService
{
    /// <summary>
    /// Gets complete SEO metadata for the current page.
    /// </summary>
    Task<BaselinePageMetaData> GetPageMetaDataAsync();

    /// <summary>
    /// Gets SEO metadata for a specific content item.
    /// </summary>
    Task<BaselinePageMetaData> GetMetaDataForContentAsync(int contentItemId);

    /// <summary>
    /// Generates meta tags HTML for the current page.
    /// </summary>
    Task<string> GenerateMetaTagsAsync();

    /// <summary>
    /// Gets Open Graph metadata for social sharing.
    /// </summary>
    Task<OpenGraphData> GetOpenGraphDataAsync();

    /// <summary>
    /// Gets Twitter Card metadata.
    /// </summary>
    Task<TwitterCardData> GetTwitterCardDataAsync();

    /// <summary>
    /// Sets page metadata from IBaseMetadata fields (Kentico reusable schema).
    /// Call this from page templates/controllers.
    /// </summary>
    /// <param name="metaFields">The IBaseMetadata from the content item.</param>
    /// <param name="ogImageUrl">Optional explicit OG image URL override.</param>
    /// <param name="pageGuid">Optional page content item GUID for dynamic OG images.</param>
    void SetFromBaseMetadata(IBaseMetadata metaFields, string? ogImageUrl = null, Guid? pageGuid = null);

    /// <summary>
    /// Sets page metadata from simple values.
    /// Use for content types that don't implement IBaseMetadata.
    /// </summary>
    /// <param name="title">Page title.</param>
    /// <param name="description">Page description.</param>
    /// <param name="ogImageUrl">Optional OG image URL.</param>
    /// <param name="noIndex">Whether to add noindex directive.</param>
    /// <param name="pageGuid">Optional page content item GUID for dynamic OG images.</param>
    void SetSimpleMetadata(string title, string? description = null, string? ogImageUrl = null, bool noIndex = false, Guid? pageGuid = null);

    /// <summary>
    /// Sets page metadata directly.
    /// </summary>
    void SetMetaData(BaselinePageMetaData metaData);

    /// <summary>
    /// Updates the page title.
    /// </summary>
    void UpdateTitle(string title);

    /// <summary>
    /// Applies title pattern from global settings (e.g., "{0} | Site Name").
    /// </summary>
    void ApplyTitlePattern(string pattern);
}

/// <summary>
/// Complete page SEO metadata (v3 Baseline version).
/// Note: Named BaselinePageMetaData to avoid conflict with v2 Core.Models.PageMetaData
/// </summary>
public class BaselinePageMetaData
{
    /// <summary>
    /// Page content item GUID for dynamic OG image generation.
    /// </summary>
    public Guid? PageGuid { get; set; }

    /// <summary>
    /// Page title for title tag.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Meta description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Meta keywords (less important for modern SEO).
    /// </summary>
    public string? Keywords { get; set; }

    /// <summary>
    /// Canonical URL.
    /// </summary>
    public string? CanonicalUrl { get; set; }

    /// <summary>
    /// Robots directive (index, noindex, follow, nofollow).
    /// </summary>
    public string? Robots { get; set; }

    /// <summary>
    /// Open Graph data.
    /// </summary>
    public OpenGraphData? OpenGraph { get; set; }

    /// <summary>
    /// Twitter Card data.
    /// </summary>
    public TwitterCardData? TwitterCard { get; set; }

    /// <summary>
    /// Alternate language URLs.
    /// </summary>
    public IEnumerable<AlternateLink> AlternateLinks { get; set; } = [];

    /// <summary>
    /// Additional custom meta tags.
    /// </summary>
    public Dictionary<string, string> CustomMeta { get; set; } = [];
}

/// <summary>
/// Open Graph metadata for social sharing.
/// </summary>
public class OpenGraphData
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Type { get; set; } = "website";
    public string? Url { get; set; }
    public string? Image { get; set; }
    public string? ImageAlt { get; set; }
    public int? ImageWidth { get; set; }
    public int? ImageHeight { get; set; }
    public string? SiteName { get; set; }
    public string? Locale { get; set; }
    public IEnumerable<string>? AlternateLocales { get; set; }
}

/// <summary>
/// Twitter Card metadata.
/// </summary>
public class TwitterCardData
{
    public string Card { get; set; } = "summary_large_image";
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Image { get; set; }
    public string? ImageAlt { get; set; }
    public string? Site { get; set; }
    public string? Creator { get; set; }
}

/// <summary>
/// Alternate language link for hreflang.
/// </summary>
public record AlternateLink(string Hreflang, string Href);
