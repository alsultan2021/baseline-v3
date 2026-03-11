namespace Baseline.Core;

/// <summary>
/// Service for generating sitemap.xml content.
/// Provides extensible sitemap generation using <see cref="ISitemapEntryProvider"/>
/// to collect entries from different modules (pages, tabs, blog posts, etc.).
/// </summary>
/// <remarks>
/// Register providers via DI: <c>services.AddScoped&lt;ISitemapEntryProvider, MyProvider&gt;()</c>.
/// All registered providers are automatically aggregated by the service.
/// The TabbedPages module registers its own <c>ISitemapEntryProvider</c> for tab URLs.
/// </remarks>
public interface ISitemapService
{
    /// <summary>
    /// Generates sitemap.xml content for the current website channel.
    /// Aggregates entries from all registered <see cref="ISitemapEntryProvider"/> instances.
    /// </summary>
    /// <param name="language">Optional language code. If null, generates for all languages.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Sitemap XML string.</returns>
    Task<string> GenerateAsync(string? language = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a sitemap index XML pointing to per-language sitemaps.
    /// Used when <c>RobotsTxtOptions.EnableLanguageSpecificSitemaps</c> is true.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Sitemap index XML string.</returns>
    Task<string> GenerateIndexAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all sitemap entries (unformatted) for programmatic access.
    /// </summary>
    /// <param name="language">Optional language filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IEnumerable<SitemapEntry>> GetEntriesAsync(
        string? language = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Provider interface for contributing sitemap entries from a specific module.
/// Register multiple implementations to aggregate entries from different sources.
/// </summary>
/// <example>
/// <code>
/// public class BlogSitemapProvider(IContentRetriever retriever) : ISitemapEntryProvider
/// {
///     public async Task&lt;IEnumerable&lt;SitemapEntry&gt;&gt; GetEntriesAsync(...)
///     {
///         var posts = await retriever.RetrievePages&lt;BlogPost&gt;(...);
///         return posts.Select(p => new SitemapEntry { ... });
///     }
/// }
/// </code>
/// </example>
public interface ISitemapEntryProvider
{
    /// <summary>
    /// Returns sitemap entries for this provider's content.
    /// </summary>
    /// <param name="language">Optional language filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IEnumerable<SitemapEntry>> GetEntriesAsync(
        string? language = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Priority of this provider (higher = entries appear first in sitemap).
    /// Default: 0.
    /// </summary>
    int Priority => 0;
}

/// <summary>
/// A single sitemap.xml entry.
/// </summary>
public record SitemapEntry
{
    /// <summary>
    /// Absolute URL (required). Must be fully qualified (https://example.com/path).
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// Last modification date (optional).
    /// </summary>
    public DateTime? LastModified { get; init; }

    /// <summary>
    /// Change frequency hint for crawlers (optional).
    /// </summary>
    public SitemapChangeFrequency? ChangeFrequency { get; init; }

    /// <summary>
    /// Priority relative to other URLs on the site (0.0–1.0). Default: 0.5.
    /// </summary>
    public double Priority { get; init; } = 0.5;

    /// <summary>
    /// Alternate language URLs for hreflang (optional).
    /// Key: language code, Value: absolute URL.
    /// </summary>
    public IReadOnlyDictionary<string, string>? AlternateLanguageUrls { get; init; }
}

/// <summary>
/// Sitemap change frequency values per the sitemap protocol.
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
