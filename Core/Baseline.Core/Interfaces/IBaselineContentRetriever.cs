using CMS.ContentEngine;
using CMS.Websites;

namespace Baseline.Core;

/// <summary>
/// Thin wrapper around XbK's IContentRetriever that adds Baseline-specific enhancements.
/// For most use cases, use IContentRetriever directly - this wrapper is for when you need
/// additional functionality like automatic structured data generation or custom caching strategies.
/// </summary>
public interface IBaselineContentRetriever
{
    /// <summary>
    /// Retrieves pages with optional structured data generation.
    /// </summary>
    /// <typeparam name="TPage">The page content type.</typeparam>
    /// <param name="parameters">Retrieval parameters.</param>
    /// <param name="includeStructuredData">Whether to generate structured data for the pages.</param>
    /// <returns>Collection of pages with optional structured data.</returns>
    Task<IEnumerable<PageWithMetadata<TPage>>> RetrievePagesWithMetadataAsync<TPage>(
        BaselineRetrievePagesParameters parameters,
        bool includeStructuredData = false) where TPage : class, IWebPageFieldsSource;

    /// <summary>
    /// Retrieves the current page with metadata.
    /// </summary>
    /// <typeparam name="TPage">The page content type.</typeparam>
    /// <param name="includeStructuredData">Whether to generate structured data.</param>
    /// <returns>The current page with metadata, or null if not found.</returns>
    Task<PageWithMetadata<TPage>?> RetrieveCurrentPageWithMetadataAsync<TPage>(
        bool includeStructuredData = false) where TPage : class, IWebPageFieldsSource;

    /// <summary>
    /// Retrieves content items with Baseline enhancements.
    /// </summary>
    /// <typeparam name="TContent">The content type.</typeparam>
    /// <param name="parameters">Retrieval parameters.</param>
    /// <returns>Collection of content items.</returns>
    Task<IEnumerable<TContent>> RetrieveContentItemsAsync<TContent>(
        BaselineRetrieveContentParameters parameters) where TContent : class, IContentItemFieldsSource;
}

/// <summary>
/// A page with its associated metadata and optional structured data.
/// </summary>
/// <typeparam name="TPage">The page content type.</typeparam>
public class PageWithMetadata<TPage> where TPage : class
{
    /// <summary>
    /// The page content.
    /// </summary>
    public required TPage Page { get; init; }

    /// <summary>
    /// SEO metadata for the page.
    /// </summary>
    public PageMetadata? Metadata { get; init; }

    /// <summary>
    /// Generated JSON-LD structured data.
    /// </summary>
    public string? StructuredDataJsonLd { get; init; }
}

/// <summary>
/// SEO metadata for a page.
/// </summary>
public class PageMetadata
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Keywords { get; set; }
    public string? CanonicalUrl { get; set; }
    public string? OgTitle { get; set; }
    public string? OgDescription { get; set; }
    public string? OgImage { get; set; }
    public string? OgType { get; set; } = "website";
    public string? TwitterCard { get; set; } = "summary_large_image";
    public bool NoIndex { get; set; }
    public bool NoFollow { get; set; }
}

/// <summary>
/// Parameters for retrieving pages with Baseline enhancements.
/// </summary>
public class BaselineRetrievePagesParameters
{
    /// <summary>
    /// Path pattern to match.
    /// </summary>
    public string? PathMatch { get; set; }

    /// <summary>
    /// Whether to include child pages.
    /// </summary>
    public bool IncludeChildren { get; set; }

    /// <summary>
    /// Maximum depth for child pages.
    /// </summary>
    public int? MaxDepth { get; set; }

    /// <summary>
    /// Language to retrieve.
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// Maximum number of results.
    /// </summary>
    public int? TopN { get; set; }

    /// <summary>
    /// Order by expression.
    /// </summary>
    public string? OrderBy { get; set; }

    /// <summary>
    /// Whether this is a preview request.
    /// </summary>
    public bool? ForPreview { get; set; }

    /// <summary>
    /// Cache settings for the retrieval operation.
    /// Aligns with Kentico's RetrievalCacheSettings.
    /// </summary>
    public BaselineCacheSettings? CacheSettings { get; set; }

    /// <summary>
    /// Whether to use language fallbacks when the requested language variant doesn't exist.
    /// Corresponds to <c>RetrievePagesParameters.UseLanguageFallbacks</c>.
    /// Default: null (uses XbK default, which is true).
    /// </summary>
    public bool? UseLanguageFallbacks { get; set; }

    /// <summary>
    /// Controls how URLs are generated when content falls back to a different language.
    /// <list type="bullet">
    /// <item><c>UseRequestedLanguage</c> (default) — URL uses the originally requested language segments.</item>
    /// <item><c>UseFallbackLanguage</c> — URL uses the actual fallback content's language segments.</item>
    /// </list>
    /// Corresponds to <c>RetrievePagesQueryParameters.SetUrlLanguageBehavior()</c>.
    /// </summary>
    public BaselineUrlLanguageBehavior? UrlLanguageBehavior { get; set; }
}

/// <summary>
/// Parameters for retrieving content items with Baseline enhancements.
/// </summary>
public class BaselineRetrieveContentParameters
{
    /// <summary>
    /// Content type name.
    /// </summary>
    public string? ContentTypeName { get; set; }

    /// <summary>
    /// Language to retrieve.
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// Maximum number of results.
    /// </summary>
    public int? TopN { get; set; }

    /// <summary>
    /// Order by expression.
    /// </summary>
    public string? OrderBy { get; set; }

    /// <summary>
    /// Filter expression.
    /// </summary>
    public string? Where { get; set; }

    /// <summary>
    /// Cache settings for the retrieval operation.
    /// Aligns with Kentico's RetrievalCacheSettings.
    /// </summary>
    public BaselineCacheSettings? CacheSettings { get; set; }
}

/// <summary>
/// Cache settings for content retrieval.
/// Aligns with Kentico's RetrievalCacheSettings pattern.
/// </summary>
public class BaselineCacheSettings
{
    /// <summary>
    /// Whether caching is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Cache expiration time. Uses global default if not specified.
    /// </summary>
    public TimeSpan? CacheExpiration { get; set; }

    /// <summary>
    /// Cache key suffix for unique identification.
    /// </summary>
    public string? CacheKeySuffix { get; set; }

    /// <summary>
    /// Whether to use sliding expiration.
    /// </summary>
    public bool UseSlidingExpiration { get; set; } = true;

    /// <summary>
    /// Disabled caching preset.
    /// </summary>
    public static BaselineCacheSettings Disabled => new() { Enabled = false };

    /// <summary>
    /// Default caching preset (uses global defaults).
    /// </summary>
    public static BaselineCacheSettings Default => new();
}

/// <summary>
/// Controls URL generation behavior when content is served from a fallback language.
/// Maps to Kentico's <c>CMS.Websites.UrlLanguageBehavior</c>.
/// </summary>
public enum BaselineUrlLanguageBehavior
{
    /// <summary>
    /// URL path uses the originally requested language (default IContentRetriever behavior).
    /// </summary>
    UseRequestedLanguage,

    /// <summary>
    /// URL path uses the language of the actual fallback content.
    /// </summary>
    UseFallbackLanguage
}
