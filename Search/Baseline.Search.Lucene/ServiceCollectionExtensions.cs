using Microsoft.Extensions.DependencyInjection;

namespace Baseline.Search.Lucene;

/// <summary>
/// Extension methods for registering Baseline Search Lucene services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Baseline Search with Lucene integration.
    /// </summary>
    /// <remarks>
    /// Available search providers:
    /// - https://github.com/Kentico/xperience-by-kentico-lucene
    /// - https://github.com/Kentico/xperience-by-kentico-algolia
    /// - https://github.com/Kentico/xperience-by-kentico-azure-ai-search
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBaselineSearchLucene(
        this IServiceCollection services,
        Action<LuceneSearchOptions>? configure = null)
    {
        var options = new LuceneSearchOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);

        // Register core search services
        services.AddScoped<ILuceneSearchRepository, LuceneSearchRepository>();
        services.AddScoped<ILuceneSearchCustomizations, DefaultLuceneSearchCustomizations>();

        // Replace the base SearchService with Lucene-specific implementation
        services.AddScoped<ISearchService, LuceneSearchService>();

        // Register web crawler services
        // See: https://github.com/Kentico/xperience-by-kentico-lucene/blob/main/docs/Scraping-web-page-content.md
        services.AddSingleton<LuceneWebScraperSanitizer>();
        services.AddHttpClient<LuceneWebCrawlerService>(client =>
        {
            if (!string.IsNullOrWhiteSpace(options.WebCrawlerBaseUrl))
            {
                client.BaseAddress = new Uri(options.WebCrawlerBaseUrl.TrimEnd('/') + "/");
            }
        });

        return services;
    }

    /// <summary>
    /// Adds a custom Lucene search customization.
    /// </summary>
    public static IServiceCollection AddLuceneSearchCustomizations<TCustomizations>(
        this IServiceCollection services)
        where TCustomizations : class, ILuceneSearchCustomizations
    {
        services.AddScoped<ILuceneSearchCustomizations, TCustomizations>();
        return services;
    }

    /// <summary>
    /// Adds a custom indexing strategy.
    /// </summary>
    public static IServiceCollection AddLuceneIndexingStrategy<TStrategy>(
        this IServiceCollection services)
        where TStrategy : class, ILuceneIndexingStrategy
    {
        services.AddScoped<ILuceneIndexingStrategy, TStrategy>();
        return services;
    }
}

/// <summary>
/// Options for Lucene search configuration.
/// </summary>
public sealed class LuceneSearchOptions
{
    /// <summary>
    /// Default Lucene index name to use for searches.
    /// </summary>
    public string DefaultIndexName { get; set; } = "SiteSearchIndex";

    /// <summary>
    /// Base URL for web crawling.
    /// </summary>
    public string WebCrawlerBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Maximum number of search results.
    /// </summary>
    public int MaxResults { get; set; } = 100;

    /// <summary>
    /// Default page size for paginated results.
    /// </summary>
    public int DefaultPageSize { get; set; } = 10;

    /// <summary>
    /// Whether to enable content scraping for indexing.
    /// </summary>
    public bool EnableWebCrawling { get; set; } = true;

    /// <summary>
    /// Content selectors to exclude from web scraping.
    /// </summary>
    public List<string> ExcludeSelectors { get; set; } = ["header", "footer", "nav", ".sidebar", "script", "style"];

    /// <summary>
    /// Content selectors to specifically include in web scraping.
    /// </summary>
    public List<string> IncludeSelectors { get; set; } = ["main", "article", ".content"];
}
