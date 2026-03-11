using Kentico.Xperience.Admin.Base.FormAnnotations;
using XperienceCommunity.ChannelSettings.Attributes;

namespace Baseline.Core.Models;

/// <summary>
/// Channel settings for Baseline Core module.
/// Configure CDN, caching, SEO, and security headers per channel.
/// These settings can be managed through the admin UI under Channel Settings.
/// </summary>
public class CoreChannelSettings
{
    #region CDN Settings

    /// <summary>
    /// Enable CDN support features.
    /// </summary>
    [XperienceSettingsData("Core.Cdn.Enabled", false)]
    [CheckBoxComponent(
        Label = "Enable CDN",
        ExplanationText = "Enable 'Bring Your Own CDN' features including cache headers, surrogate keys, and edge optimizations.",
        Order = 1)]
    public virtual bool CdnEnabled { get; set; } = false;

    /// <summary>
    /// CDN provider identifier.
    /// </summary>
    [XperienceSettingsData("Core.Cdn.Provider", "")]
    [DropDownComponent(
        Label = "CDN Provider",
        ExplanationText = "Select your CDN provider for provider-specific optimizations.",
        DataProviderType = typeof(CdnProviderDataProvider),
        Order = 2)]
    [VisibleIfTrue(nameof(CdnEnabled))]
    public virtual string CdnProvider { get; set; } = string.Empty;

    /// <summary>
    /// The origin server base URL.
    /// </summary>
    [XperienceSettingsData("Core.Cdn.OriginBaseUrl", "")]
    [TextInputComponent(
        Label = "Origin Base URL",
        ExplanationText = "The origin server URL that the CDN pulls content from (e.g., https://origin.example.com).",
        Order = 3)]
    [VisibleIfTrue(nameof(CdnEnabled))]
    public virtual string CdnOriginBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// The CDN edge URL.
    /// </summary>
    [XperienceSettingsData("Core.Cdn.EdgeBaseUrl", "")]
    [TextInputComponent(
        Label = "Edge Base URL",
        ExplanationText = "The CDN edge URL that users access (e.g., https://cdn.example.com).",
        Order = 4)]
    [VisibleIfTrue(nameof(CdnEnabled))]
    public virtual string CdnEdgeBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Enable cache control headers.
    /// </summary>
    [XperienceSettingsData("Core.Cdn.EnableCacheHeaders", true)]
    [CheckBoxComponent(
        Label = "Enable Cache Headers",
        ExplanationText = "Add Cache-Control, Surrogate-Control, and CDN-specific headers to responses.",
        Order = 5)]
    [VisibleIfTrue(nameof(CdnEnabled))]
    public virtual bool CdnEnableCacheHeaders { get; set; } = true;

    /// <summary>
    /// Default page cache TTL in seconds.
    /// </summary>
    [XperienceSettingsData("Core.Cdn.DefaultPageTtlSeconds", 300)]
    [NumberInputComponent(
        Label = "Page Cache TTL (seconds)",
        ExplanationText = "Default cache duration for dynamic pages. Default: 300 (5 minutes).",
        Order = 6)]
    [VisibleIfTrue(nameof(CdnEnabled))]
    public virtual int CdnDefaultPageTtlSeconds { get; set; } = 300;

    /// <summary>
    /// Static asset cache TTL in seconds.
    /// </summary>
    [XperienceSettingsData("Core.Cdn.StaticAssetTtlSeconds", 31536000)]
    [NumberInputComponent(
        Label = "Static Asset TTL (seconds)",
        ExplanationText = "Cache duration for static assets (CSS, JS, images). Default: 31536000 (1 year).",
        Order = 7)]
    [VisibleIfTrue(nameof(CdnEnabled))]
    public virtual int CdnStaticAssetTtlSeconds { get; set; } = 31536000;

    /// <summary>
    /// Media library cache TTL in seconds.
    /// </summary>
    [XperienceSettingsData("Core.Cdn.MediaLibraryTtlSeconds", 2592000)]
    [NumberInputComponent(
        Label = "Media Library TTL (seconds)",
        ExplanationText = "Cache duration for media library files. Default: 2592000 (30 days).",
        Order = 8)]
    [VisibleIfTrue(nameof(CdnEnabled))]
    public virtual int CdnMediaLibraryTtlSeconds { get; set; } = 2592000;

    /// <summary>
    /// Enable stale-while-revalidate.
    /// </summary>
    [XperienceSettingsData("Core.Cdn.EnableStaleWhileRevalidate", true)]
    [CheckBoxComponent(
        Label = "Enable Stale-While-Revalidate",
        ExplanationText = "Serve stale content while revalidating in the background for better performance.",
        Order = 9)]
    [VisibleIfTrue(nameof(CdnEnabled))]
    public virtual bool CdnEnableStaleWhileRevalidate { get; set; } = true;

    /// <summary>
    /// Enable cache tags for targeted purging.
    /// </summary>
    [XperienceSettingsData("Core.Cdn.EnableCacheTags", true)]
    [CheckBoxComponent(
        Label = "Enable Cache Tags",
        ExplanationText = "Generate surrogate keys/cache tags for targeted cache purging when content changes.",
        Order = 10)]
    [VisibleIfTrue(nameof(CdnEnabled))]
    public virtual bool CdnEnableCacheTags { get; set; } = true;

    /// <summary>
    /// Cache tag header name.
    /// </summary>
    [XperienceSettingsData("Core.Cdn.CacheTagHeader", "Surrogate-Key")]
    [DropDownComponent(
        Label = "Cache Tag Header",
        ExplanationText = "Header name for cache tags. Fastly uses 'Surrogate-Key', Cloudflare uses 'Cache-Tag'.",
        DataProviderType = typeof(CacheTagHeaderDataProvider),
        Order = 11)]
    [VisibleIfTrue(nameof(CdnEnabled))]
    public virtual string CdnCacheTagHeader { get; set; } = "Surrogate-Key";

    /// <summary>
    /// Cache tag prefix.
    /// </summary>
    [XperienceSettingsData("Core.Cdn.CacheTagPrefix", "")]
    [TextInputComponent(
        Label = "Cache Tag Prefix",
        ExplanationText = "Prefix for cache tags to avoid collisions in multi-site setups (e.g., 'mysite_').",
        Order = 12)]
    [VisibleIfTrue(nameof(CdnEnabled))]
    public virtual string CdnCacheTagPrefix { get; set; } = string.Empty;

    #endregion

    #region Security Headers

    /// <summary>
    /// Enable security headers middleware.
    /// </summary>
    [XperienceSettingsData("Core.SecurityHeaders.Enabled", true)]
    [CheckBoxComponent(
        Label = "Enable Security Headers",
        ExplanationText = "Add security headers (X-Frame-Options, CSP, HSTS, etc.) to responses.",
        Order = 20)]
    public virtual bool SecurityHeadersEnabled { get; set; } = true;

    /// <summary>
    /// X-Frame-Options header value.
    /// </summary>
    [XperienceSettingsData("Core.SecurityHeaders.XFrameOptions", "DENY")]
    [DropDownComponent(
        Label = "X-Frame-Options",
        ExplanationText = "Prevent clickjacking by controlling iframe embedding.",
        DataProviderType = typeof(XFrameOptionsDataProvider),
        Order = 21)]
    [VisibleIfTrue(nameof(SecurityHeadersEnabled))]
    public virtual string SecurityHeadersXFrameOptions { get; set; } = "DENY";

    /// <summary>
    /// Referrer-Policy header value.
    /// </summary>
    [XperienceSettingsData("Core.SecurityHeaders.ReferrerPolicy", "strict-origin-when-cross-origin")]
    [DropDownComponent(
        Label = "Referrer Policy",
        ExplanationText = "Control how much referrer information is sent with requests.",
        DataProviderType = typeof(ReferrerPolicyDataProvider),
        Order = 22)]
    [VisibleIfTrue(nameof(SecurityHeadersEnabled))]
    public virtual string SecurityHeadersReferrerPolicy { get; set; } = "strict-origin-when-cross-origin";

    /// <summary>
    /// Enable HSTS header.
    /// </summary>
    [XperienceSettingsData("Core.SecurityHeaders.StrictTransportSecurity", true)]
    [CheckBoxComponent(
        Label = "Enable HSTS",
        ExplanationText = "Add Strict-Transport-Security header to enforce HTTPS.",
        Order = 23)]
    [VisibleIfTrue(nameof(SecurityHeadersEnabled))]
    public virtual bool SecurityHeadersStrictTransportSecurity { get; set; } = true;

    /// <summary>
    /// HSTS max-age in seconds.
    /// </summary>
    [XperienceSettingsData("Core.SecurityHeaders.HstsMaxAgeSeconds", 31536000)]
    [NumberInputComponent(
        Label = "HSTS Max Age (seconds)",
        ExplanationText = "How long browsers should remember to use HTTPS. Default: 31536000 (1 year).",
        Order = 24)]
    [VisibleIfTrue(nameof(SecurityHeadersEnabled))]
    public virtual int SecurityHeadersHstsMaxAgeSeconds { get; set; } = 31536000;

    /// <summary>
    /// Content-Security-Policy header value.
    /// </summary>
    [XperienceSettingsData("Core.SecurityHeaders.ContentSecurityPolicy", "")]
    [TextAreaComponent(
        Label = "Content Security Policy",
        ExplanationText = "CSP header value to control which resources can be loaded. Leave empty to disable.",
        Order = 25)]
    [VisibleIfTrue(nameof(SecurityHeadersEnabled))]
    public virtual string SecurityHeadersContentSecurityPolicy { get; set; } = string.Empty;

    #endregion

    #region Output Cache

    /// <summary>
    /// Enable output cache.
    /// </summary>
    [XperienceSettingsData("Core.OutputCache.Enabled", true)]
    [CheckBoxComponent(
        Label = "Enable Output Cache",
        ExplanationText = "Enable server-side output caching for improved performance.",
        Order = 30)]
    public virtual bool OutputCacheEnabled { get; set; } = true;

    /// <summary>
    /// Default cache expiration in minutes.
    /// </summary>
    [XperienceSettingsData("Core.OutputCache.DefaultExpirationMinutes", 10)]
    [NumberInputComponent(
        Label = "Default Expiration (minutes)",
        ExplanationText = "Default output cache expiration time in minutes.",
        Order = 31)]
    [VisibleIfTrue(nameof(OutputCacheEnabled))]
    public virtual int OutputCacheDefaultExpirationMinutes { get; set; } = 10;

    #endregion

    #region Compression

    /// <summary>
    /// Enable response compression.
    /// </summary>
    [XperienceSettingsData("Core.Compression.Enabled", true)]
    [CheckBoxComponent(
        Label = "Enable Compression",
        ExplanationText = "Enable Brotli and Gzip response compression for smaller payloads.",
        Order = 40)]
    public virtual bool CompressionEnabled { get; set; } = true;

    #endregion

    #region SEO Features

    /// <summary>
    /// Enable structured data generation.
    /// </summary>
    [XperienceSettingsData("Core.Seo.EnableStructuredData", true)]
    [CheckBoxComponent(
        Label = "Enable Structured Data",
        ExplanationText = "Generate JSON-LD structured data for search engine optimization.",
        Order = 50)]
    public virtual bool SeoEnableStructuredData { get; set; } = true;

    /// <summary>
    /// Enable robots.txt endpoint.
    /// </summary>
    [XperienceSettingsData("Core.Seo.EnableRobotsTxt", true)]
    [CheckBoxComponent(
        Label = "Enable robots.txt",
        ExplanationText = "Generate robots.txt endpoint for search engine crawlers.",
        Order = 51)]
    public virtual bool SeoEnableRobotsTxt { get; set; } = true;

    /// <summary>
    /// Enable llms.txt endpoint.
    /// </summary>
    [XperienceSettingsData("Core.Seo.EnableLlmsTxt", true)]
    [CheckBoxComponent(
        Label = "Enable llms.txt",
        ExplanationText = "Generate llms.txt endpoint for AI crawlers and assistants.",
        Order = 52)]
    public virtual bool SeoEnableLlmsTxt { get; set; } = true;

    #endregion

    #region Sitemap

    /// <summary>
    /// Enable XML sitemap generation.
    /// </summary>
    [XperienceSettingsData("Core.Sitemap.Enabled", true)]
    [CheckBoxComponent(
        Label = "Enable Sitemap",
        ExplanationText = "Enable XML sitemap generation at /sitemap.xml for search engine crawlers.",
        Order = 55)]
    public virtual bool SitemapEnabled { get; set; } = true;

    /// <summary>
    /// Sitemap base URL override.
    /// </summary>
    [XperienceSettingsData("Core.Sitemap.BaseUrl", "")]
    [TextInputComponent(
        Label = "Base URL Override",
        ExplanationText = "Optional base URL for sitemap links. Leave empty to use the current request URL.",
        Order = 56)]
    [VisibleIfTrue(nameof(SitemapEnabled))]
    public virtual string SitemapBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Include sitemap index for multiple sitemaps.
    /// </summary>
    [XperienceSettingsData("Core.Sitemap.EnableSitemapIndex", true)]
    [CheckBoxComponent(
        Label = "Enable Sitemap Index",
        ExplanationText = "Generate a sitemap index that references individual sitemaps for large sites.",
        Order = 57)]
    [VisibleIfTrue(nameof(SitemapEnabled))]
    public virtual bool SitemapEnableSitemapIndex { get; set; } = true;

    /// <summary>
    /// Maximum URLs per sitemap file.
    /// </summary>
    [XperienceSettingsData("Core.Sitemap.MaxUrlsPerSitemap", 50000)]
    [NumberInputComponent(
        Label = "Max URLs per Sitemap",
        ExplanationText = "Maximum number of URLs in each sitemap file. Google recommends 50,000 max.",
        Order = 58)]
    [VisibleIfTrue(nameof(SitemapEnabled))]
    public virtual int SitemapMaxUrlsPerSitemap { get; set; } = 50000;

    /// <summary>
    /// Include language-specific sitemaps (hreflang).
    /// </summary>
    [XperienceSettingsData("Core.Sitemap.EnableHreflang", true)]
    [CheckBoxComponent(
        Label = "Enable Hreflang Links",
        ExplanationText = "Include hreflang alternate links for multilingual sites.",
        Order = 59)]
    [VisibleIfTrue(nameof(SitemapEnabled))]
    public virtual bool SitemapEnableHreflang { get; set; } = true;

    /// <summary>
    /// Include images in sitemap.
    /// </summary>
    [XperienceSettingsData("Core.Sitemap.IncludeImages", true)]
    [CheckBoxComponent(
        Label = "Include Images",
        ExplanationText = "Include image information in sitemap entries for Google Image search.",
        Order = 60)]
    [VisibleIfTrue(nameof(SitemapEnabled))]
    public virtual bool SitemapIncludeImages { get; set; } = true;

    /// <summary>
    /// Include news articles in sitemap.
    /// </summary>
    [XperienceSettingsData("Core.Sitemap.IncludeNews", false)]
    [CheckBoxComponent(
        Label = "Include News Sitemap",
        ExplanationText = "Generate a separate news sitemap for Google News (requires news content types).",
        Order = 61)]
    [VisibleIfTrue(nameof(SitemapEnabled))]
    public virtual bool SitemapIncludeNews { get; set; } = false;

    /// <summary>
    /// Include video content in sitemap.
    /// </summary>
    [XperienceSettingsData("Core.Sitemap.IncludeVideos", false)]
    [CheckBoxComponent(
        Label = "Include Video Sitemap",
        ExplanationText = "Generate a separate video sitemap for video content discovery.",
        Order = 62)]
    [VisibleIfTrue(nameof(SitemapEnabled))]
    public virtual bool SitemapIncludeVideos { get; set; } = false;

    /// <summary>
    /// Default change frequency for sitemap entries.
    /// </summary>
    [XperienceSettingsData("Core.Sitemap.DefaultChangeFrequency", "weekly")]
    [DropDownComponent(
        Label = "Default Change Frequency",
        ExplanationText = "Default change frequency hint for search engines.",
        DataProviderType = typeof(SitemapChangeFrequencyDataProvider),
        Order = 63)]
    [VisibleIfTrue(nameof(SitemapEnabled))]
    public virtual string SitemapDefaultChangeFrequency { get; set; } = "weekly";

    /// <summary>
    /// Default priority for sitemap entries.
    /// </summary>
    [XperienceSettingsData("Core.Sitemap.DefaultPriority", "0.5")]
    [DropDownComponent(
        Label = "Default Priority",
        ExplanationText = "Default priority hint for search engines (0.0 to 1.0).",
        DataProviderType = typeof(SitemapPriorityDataProvider),
        Order = 64)]
    [VisibleIfTrue(nameof(SitemapEnabled))]
    public virtual string SitemapDefaultPriority { get; set; } = "0.5";

    /// <summary>
    /// Sitemap cache duration in minutes.
    /// </summary>
    [XperienceSettingsData("Core.Sitemap.CacheDurationMinutes", 60)]
    [NumberInputComponent(
        Label = "Cache Duration (minutes)",
        ExplanationText = "How long to cache the sitemap before regenerating.",
        Order = 65)]
    [VisibleIfTrue(nameof(SitemapEnabled))]
    public virtual int SitemapCacheDurationMinutes { get; set; } = 60;

    /// <summary>
    /// Excluded URL patterns (comma-separated).
    /// </summary>
    [XperienceSettingsData("Core.Sitemap.ExcludedPatterns", "")]
    [TextAreaComponent(
        Label = "Excluded URL Patterns",
        ExplanationText = "Comma-separated list of URL patterns to exclude from sitemap (e.g., /admin/*, /private/*).",
        Order = 66)]
    [VisibleIfTrue(nameof(SitemapEnabled))]
    public virtual string SitemapExcludedPatterns { get; set; } = string.Empty;

    /// <summary>
    /// Content types to include in the sitemap.
    /// When empty, all page content types are included.
    /// </summary>
    [XperienceSettingsData("Core.Sitemap.IncludedContentTypes", "[]")]
    [GeneralSelectorComponent(
        dataProviderType: typeof(SitemapContentTypesDataProvider),
        Label = "Included Content Types",
        ExplanationText = "Select which page content types to include in the sitemap. Leave empty to include all page types.",
        Order = 67,
        Placeholder = "Select content types...")]
    [VisibleIfTrue(nameof(SitemapEnabled))]
    public virtual IEnumerable<string> SitemapIncludedContentTypes { get; set; } = [];

    #endregion

    #region Responsive Images

    /// <summary>
    /// Enable responsive images.
    /// </summary>
    [XperienceSettingsData("Core.ResponsiveImages.Enabled", true)]
    [CheckBoxComponent(
        Label = "Enable Responsive Images",
        ExplanationText = "Enable responsive image tag helper with srcset generation.",
        Order = 60)]
    public virtual bool ResponsiveImagesEnabled { get; set; } = true;

    /// <summary>
    /// Default image quality.
    /// </summary>
    [XperienceSettingsData("Core.ResponsiveImages.DefaultQuality", 80)]
    [NumberInputComponent(
        Label = "Image Quality",
        ExplanationText = "Default image quality (1-100). Higher values mean better quality but larger files.",
        Order = 61)]
    [VisibleIfTrue(nameof(ResponsiveImagesEnabled))]
    public virtual int ResponsiveImagesDefaultQuality { get; set; } = 80;

    /// <summary>
    /// Default image format.
    /// </summary>
    [XperienceSettingsData("Core.ResponsiveImages.DefaultFormat", "webp")]
    [DropDownComponent(
        Label = "Default Image Format",
        ExplanationText = "Default format for responsive images. WebP offers best compression.",
        DataProviderType = typeof(ImageFormatDataProvider),
        Order = 62)]
    [VisibleIfTrue(nameof(ResponsiveImagesEnabled))]
    public virtual string ResponsiveImagesDefaultFormat { get; set; } = "webp";

    #endregion
}
