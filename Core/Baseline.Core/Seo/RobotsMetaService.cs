using Kentico.Content.Web.Mvc.Routing;
using Microsoft.Extensions.Logging;

namespace Baseline.Core.Seo;

/// <summary>
/// Service for generating advanced robots meta tags based on page type and content.
/// Provides granular control over search engine indexing and crawling directives.
/// Supports multi-language scenarios via Kentico's IPreferredLanguageRetriever.
/// </summary>
public interface IRobotsMetaService
{
    /// <summary>
    /// Generates a robots meta tag based on page type and content state.
    /// </summary>
    /// <param name="pageType">The content type of the page (e.g., "blog", "account", "search").</param>
    /// <param name="isPreview">Whether the page is being viewed in preview mode.</param>
    /// <param name="isArchived">Whether the content is archived.</param>
    /// <returns>Robots meta tag configuration.</returns>
    RobotsMetaTag GenerateRobotsMetaTag(string pageType, bool isPreview = false, bool isArchived = false);

    /// <summary>
    /// Generates a robots meta tag from SeoMetadata.
    /// </summary>
    /// <param name="metadata">SEO metadata containing robots directive.</param>
    /// <param name="isPreview">Whether the page is being viewed in preview mode.</param>
    /// <returns>Robots meta tag configuration.</returns>
    RobotsMetaTag FromSeoMetadata(SeoMetadata metadata, bool isPreview = false);

    /// <summary>
    /// Generates hreflang link tags for the current page in all available languages.
    /// </summary>
    /// <param name="alternateUrls">Dictionary of language code to URL mappings.</param>
    /// <param name="currentLanguage">The current language code.</param>
    /// <returns>Collection of hreflang link elements.</returns>
    IEnumerable<HreflangLink> GenerateHreflangLinks(
        IDictionary<string, string> alternateUrls,
        string currentLanguage);

    /// <summary>
    /// Gets the current language from the request context.
    /// </summary>
    /// <returns>Current language code or null if not in a web context.</returns>
    string? GetCurrentLanguage();
}

/// <summary>
/// Represents a robots meta tag configuration with support for all standard directives.
/// See: https://developers.google.com/search/docs/crawling-indexing/robots-meta-tag
/// </summary>
public sealed record RobotsMetaTag
{
    /// <summary>Allows search engines to index this page.</summary>
    public bool Index { get; init; } = true;

    /// <summary>Allows search engines to follow links on this page.</summary>
    public bool Follow { get; init; } = true;

    /// <summary>Maximum text snippet length (-1 for no limit, 0 for none).</summary>
    public int MaxSnippet { get; init; } = -1;

    /// <summary>Maximum image preview size ("none", "standard", "large").</summary>
    public string MaxImagePreview { get; init; } = "large";

    /// <summary>Maximum video preview length in seconds (-1 for no limit).</summary>
    public int MaxVideoPreview { get; init; } = -1;

    /// <summary>Prevents search engines from caching the page.</summary>
    public bool NoArchive { get; init; }

    /// <summary>Prevents translation of the page.</summary>
    public bool NoTranslate { get; init; }

    /// <summary>Generates the robots meta tag content string.</summary>
    public override string ToString()
    {
        var parts = new List<string>
        {
            Index ? "index" : "noindex",
            Follow ? "follow" : "nofollow"
        };

        if (NoArchive)
        {
            parts.Add("noarchive");
        }

        if (NoTranslate)
        {
            parts.Add("notranslate");
        }

        if (MaxSnippet >= 0)
        {
            parts.Add($"max-snippet:{MaxSnippet}");
        }

        if (!string.IsNullOrWhiteSpace(MaxImagePreview))
        {
            parts.Add($"max-image-preview:{MaxImagePreview}");
        }

        if (MaxVideoPreview >= 0)
        {
            parts.Add($"max-video-preview:{MaxVideoPreview}");
        }

        return string.Join(", ", parts);
    }

    /// <summary>
    /// Returns the HTML meta tag string.
    /// </summary>
    public string ToHtmlMetaTag() => $"<meta name=\"robots\" content=\"{this}\" />";
}

/// <summary>
/// Default implementation of robots meta tag generation with page-type specific rules.
/// Supports multi-language via Kentico's IPreferredLanguageRetriever.
/// </summary>
internal sealed class RobotsMetaService(
    IPreferredLanguageRetriever preferredLanguageRetriever,
    ILogger<RobotsMetaService> logger) : IRobotsMetaService
{
    /// <summary>
    /// Page types that should not be indexed (transactional pages).
    /// </summary>
    private static readonly HashSet<string> NoIndexPageTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "checkout", "cart", "account", "login", "register", "password", "profile", "settings"
    };

    /// <summary>
    /// Page types with full content indexing (blog, articles, etc.).
    /// </summary>
    private static readonly HashSet<string> ContentPageTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "blog", "article", "news", "post", "restaurant", "reception", "event", "product"
    };

    public RobotsMetaTag GenerateRobotsMetaTag(string pageType, bool isPreview = false, bool isArchived = false)
    {
        // Preview pages should not be indexed
        if (isPreview)
        {
            return new RobotsMetaTag { Index = false, Follow = false };
        }

        // Archived content - no index but follow links
        if (isArchived)
        {
            return new RobotsMetaTag { Index = false, Follow = true };
        }

        var normalizedPageType = pageType?.ToLowerInvariant() ?? string.Empty;

        // Transactional pages - don't index
        if (NoIndexPageTypes.Contains(normalizedPageType))
        {
            return new RobotsMetaTag { Index = false, Follow = false };
        }

        // Search pages - index with limited snippet
        if (normalizedPageType == "search")
        {
            return new RobotsMetaTag { Index = true, Follow = true, MaxSnippet = 100 };
        }

        // Content pages - full indexing
        if (ContentPageTypes.Contains(normalizedPageType))
        {
            return new RobotsMetaTag { Index = true, Follow = true, MaxSnippet = 320, MaxImagePreview = "large" };
        }

        // Default - standard indexing
        return new RobotsMetaTag { Index = true, Follow = true };
    }

    public RobotsMetaTag FromSeoMetadata(SeoMetadata metadata, bool isPreview = false)
    {
        // Preview mode always blocks indexing
        if (isPreview)
        {
            return new RobotsMetaTag { Index = false, Follow = false };
        }

        // If metadata has explicit robots directive, parse it
        if (!string.IsNullOrEmpty(metadata.Robots))
        {
            var robots = metadata.Robots.ToLowerInvariant();
            return new RobotsMetaTag
            {
                Index = !robots.Contains("noindex"),
                Follow = !robots.Contains("nofollow"),
                NoArchive = robots.Contains("noarchive"),
                NoTranslate = robots.Contains("notranslate")
            };
        }

        // Default to indexing if in sitemap
        return new RobotsMetaTag
        {
            Index = metadata.IncludeInSitemap,
            Follow = true
        };
    }

    public IEnumerable<HreflangLink> GenerateHreflangLinks(
        IDictionary<string, string> alternateUrls,
        string currentLanguage)
    {
        if (alternateUrls == null || alternateUrls.Count == 0)
        {
            yield break;
        }

        // Output hreflang for each language variant
        foreach (var (language, url) in alternateUrls)
        {
            yield return new HreflangLink
            {
                Hreflang = language,
                Href = url,
                IsCurrent = string.Equals(language, currentLanguage, StringComparison.OrdinalIgnoreCase)
            };
        }

        // Add x-default pointing to primary language (first in dictionary) or current
        var defaultUrl = alternateUrls.Values.FirstOrDefault();
        if (string.IsNullOrEmpty(defaultUrl) && alternateUrls.TryGetValue(currentLanguage, out var currentUrl))
        {
            defaultUrl = currentUrl;
        }

        if (!string.IsNullOrEmpty(defaultUrl))
        {
            yield return new HreflangLink
            {
                Hreflang = "x-default",
                Href = defaultUrl,
                IsCurrent = false
            };
        }
    }

    public string? GetCurrentLanguage()
    {
        try
        {
            return preferredLanguageRetriever.Get();
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "RobotsMetaService: unable to retrieve preferred language (may not be in web context)");
            return null;
        }
    }
}

/// <summary>
/// Represents an hreflang link element for multi-language SEO.
/// </summary>
public sealed record HreflangLink
{
    /// <summary>The language/region code (e.g., "en", "fr", "en-US", "x-default").</summary>
    public required string Hreflang { get; init; }

    /// <summary>The URL for this language variant.</summary>
    public required string Href { get; init; }

    /// <summary>Whether this is the current language.</summary>
    public bool IsCurrent { get; init; }

    /// <summary>Generates the HTML link element.</summary>
    public string ToHtmlLinkTag() => $"<link rel=\"alternate\" hreflang=\"{Hreflang}\" href=\"{Href}\" />";
}
