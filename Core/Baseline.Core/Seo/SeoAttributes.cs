using Baseline.Core;

namespace Baseline.Core.Seo;

/// <summary>
/// Marks a property as a source for SEO metadata.
/// When applied, the SEO service will automatically extract metadata from this property.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
public sealed class SeoFieldAttribute : Attribute
{
    /// <summary>
    /// The type of SEO metadata this property provides.
    /// </summary>
    public SeoFieldType FieldType { get; }

    /// <summary>
    /// Creates a new SeoFieldAttribute with the specified field type.
    /// </summary>
    /// <param name="fieldType">The SEO field type this property maps to.</param>
    public SeoFieldAttribute(SeoFieldType fieldType)
    {
        FieldType = fieldType;
    }
}

/// <summary>
/// Marks a content type as having SEO metadata from IBaseMetadata schema.
/// Enables automatic SEO extraction using conventions.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = true, AllowMultiple = false)]
public sealed class SeoEnabledAttribute : Attribute
{
    /// <summary>
    /// Whether to auto-generate structured data (JSON-LD) for this content type.
    /// Default: true
    /// </summary>
    public bool GenerateStructuredData { get; set; } = true;

    /// <summary>
    /// The Schema.org type for structured data (e.g., "Article", "Product", "WebPage").
    /// Default: "WebPage"
    /// </summary>
    public string SchemaType { get; set; } = "WebPage";

    /// <summary>
    /// Whether to include in sitemap.xml by default.
    /// Default: true
    /// </summary>
    public bool IncludeInSitemap { get; set; } = true;

    /// <summary>
    /// Default change frequency for sitemap.
    /// </summary>
    public SitemapChangeFrequency ChangeFrequency { get; set; } = SitemapChangeFrequency.Weekly;

    /// <summary>
    /// Default priority for sitemap (0.0 to 1.0).
    /// </summary>
    public double Priority { get; set; } = 0.5;
}

/// <summary>
/// Types of SEO metadata fields.
/// </summary>
public enum SeoFieldType
{
    /// <summary>Page title for &lt;title&gt; tag</summary>
    Title,

    /// <summary>Meta description</summary>
    Description,

    /// <summary>Open Graph title</summary>
    OgTitle,

    /// <summary>Open Graph description</summary>
    OgDescription,

    /// <summary>Open Graph image URL</summary>
    OgImage,

    /// <summary>Twitter card title</summary>
    TwitterTitle,

    /// <summary>Twitter card description</summary>
    TwitterDescription,

    /// <summary>Twitter card image</summary>
    TwitterImage,

    /// <summary>Canonical URL</summary>
    CanonicalUrl,

    /// <summary>Robots meta tag content</summary>
    Robots,

    /// <summary>Structured data / JSON-LD</summary>
    StructuredData,

    /// <summary>Keywords (deprecated but sometimes needed)</summary>
    Keywords,

    /// <summary>Author name for articles</summary>
    Author,

    /// <summary>Published date for articles</summary>
    PublishedDate,

    /// <summary>Modified date for articles</summary>
    ModifiedDate,

    /// <summary>Article section/category</summary>
    Section,

    /// <summary>Tags/categories for the content</summary>
    Tags
}

/// <summary>
/// Sitemap change frequency values.
/// </summary>
public enum SitemapChangeFrequency
{
    Always,
    Hourly,
    Daily,
    Weekly,
    Monthly,
    Yearly,
    Never
}

/// <summary>
/// Interface for content types that provide SEO metadata.
/// Implement this interface or use IBaseMetadata schema for automatic SEO support.
/// </summary>
public interface ISeoMetadataProvider
{
    /// <summary>Page title</summary>
    string? SeoTitle { get; }

    /// <summary>Meta description</summary>
    string? SeoDescription { get; }

    /// <summary>Open Graph image URL or asset</summary>
    string? SeoOgImage { get; }

    /// <summary>Canonical URL override (null = auto-generate)</summary>
    string? SeoCanonicalUrl { get; }

    /// <summary>Robots meta content (null = default)</summary>
    string? SeoRobots { get; }

    /// <summary>Whether to include in sitemap</summary>
    bool SeoIncludeInSitemap { get; }
}

/// <summary>
/// Result of SEO metadata extraction from a content item.
/// Consolidates all SEO, Open Graph, and Twitter Card metadata.
/// </summary>
public sealed record SeoMetadata
{
    // Core SEO fields
    public string Title { get; init; } = "";
    public string Description { get; init; } = "";
    public string? CanonicalUrl { get; init; }
    public string? Robots { get; init; }

    /// <summary>
    /// Convenience property - returns true if Robots contains "noindex".
    /// </summary>
    public bool NoIndex => Robots?.Contains("noindex", StringComparison.OrdinalIgnoreCase) ?? false;

    // Open Graph fields
    public string? OgTitle { get; init; }
    public string? OgDescription { get; init; }
    public string? OgImage { get; init; }
    public string? OgImageAlt { get; init; }
    public string? OgType { get; init; } = "website";
    public string? OgSiteName { get; init; }
    public string? OgLocale { get; init; }

    // Twitter Card fields
    public string? TwitterCard { get; init; } = "summary_large_image";
    public string? TwitterTitle { get; init; }
    public string? TwitterDescription { get; init; }
    public string? TwitterImage { get; init; }
    public string? TwitterSite { get; init; }
    public string? TwitterCreator { get; init; }

    // Article/Content metadata
    public string? Author { get; init; }
    public DateTimeOffset? PublishedDate { get; init; }
    public DateTimeOffset? ModifiedDate { get; init; }
    public IReadOnlyList<string> Keywords { get; init; } = [];

    // Structured data
    public string? StructuredDataJson { get; init; }

    // Sitemap settings
    public bool IncludeInSitemap { get; init; } = true;
    public SitemapChangeFrequency SitemapChangeFrequency { get; init; } = SitemapChangeFrequency.Weekly;
    public double SitemapPriority { get; init; } = 0.5;

    /// <summary>
    /// Default empty metadata.
    /// </summary>
    public static SeoMetadata Default => new();

    /// <summary>
    /// Creates SeoMetadata from an IBaseMetadata instance.
    /// </summary>
    /// <param name="meta">The base metadata from Kentico reusable schema.</param>
    /// <param name="ogImageResolver">Optional function to resolve OG image URL from content.</param>
    /// <returns>Populated SeoMetadata record.</returns>
    public static SeoMetadata FromBaseMetadata(IBaseMetadata meta, Func<IEnumerable<IGenericHasImage>?, string?>? ogImageResolver = null)
    {
        var title = meta.MetaData_Title ?? meta.MetaData_PageName ?? "";
        var description = meta.MetaData_Description ?? "";
        var keywords = string.IsNullOrEmpty(meta.MetaData_Keywords)
            ? []
            : meta.MetaData_Keywords.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

        string? ogImage = null;
        if (ogImageResolver != null)
        {
            ogImage = ogImageResolver(meta.MetaData_OGImage);
        }

        return new SeoMetadata
        {
            Title = title,
            Description = description,
            Keywords = keywords,
            Robots = meta.MetaData_NoIndex ? "noindex, nofollow" : null,
            OgTitle = title,
            OgDescription = description,
            OgImage = ogImage,
            IncludeInSitemap = !meta.MetaData_NoIndex
        };
    }
}
