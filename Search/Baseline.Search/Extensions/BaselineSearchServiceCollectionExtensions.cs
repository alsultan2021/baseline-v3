using Microsoft.Extensions.DependencyInjection;

namespace Baseline.Search;

/// <summary>
/// Extension methods for registering Baseline v3 Search services.
/// </summary>
public static class BaselineSearchServiceCollectionExtensions
{
    /// <summary>
    /// Adds Baseline v3 Search services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action for Search options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddBaselineSearch(options =>
    /// {
    ///     options.Provider = "Lucene";
    ///     options.EnableFacets = true;
    ///     options.EnableSuggestions = true;
    ///     options.EnableAnalytics = true;
    ///     options.EnableSemanticSearch = false;
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddBaselineSearch(
        this IServiceCollection services,
        Action<BaselineSearchOptions>? configure = null)
    {
        // Register options using the Options pattern
        services.AddOptions<BaselineSearchOptions>()
            .Configure(opt => configure?.Invoke(opt));

        // Build options for conditional registration
        var options = new BaselineSearchOptions();
        configure?.Invoke(options);

        // Register facet service
        if (options.EnableFacets)
        {
            services.AddScoped<IFacetService, FacetService>();
        }

        // Register analytics service
        if (options.EnableAnalytics)
        {
            services.AddSingleton<ISearchAnalyticsService, SearchAnalyticsService>();
        }

        // Register boosting service
        services.AddScoped<ISearchBoostingService, SearchBoostingService>();

        // Register index generation storage with retention
        services.AddSingleton<IIndexGenerationService, IndexGenerationStorageService>();

        // Register semantic search service (if enabled)
        if (options.EnableSemanticSearch)
        {
            // Register the embedding provider based on config
            switch (options.Semantic.EmbeddingProvider)
            {
                case "AzureOpenAI":
                    services.AddHttpClient<IEmbeddingProvider, AzureOpenAIEmbeddingProvider>();
                    services.AddSingleton(options.Semantic);
                    break;
                case "OpenAI":
                    services.AddHttpClient<IEmbeddingProvider, OpenAIEmbeddingProvider>();
                    services.AddSingleton(options.Semantic);
                    break;
                default:
                    services.AddSingleton(options.Semantic);
                    services.AddSingleton<IEmbeddingProvider, PseudoEmbeddingProvider>();
                    break;
            }

            services.AddSingleton<ISemanticSearchService, SemanticSearchService>();
        }

        // Register no-op index service as default (provider packages replace this)
        services.AddScoped<ISearchIndexService, NoOpSearchIndexService>();

        // Register member authorization filter (uses HttpContext for auth checks)
        services.AddScoped<IMemberAuthorizationFilter, MemberAuthorizationFilterService>();

        // Register the base search service
        // Note: Actual search provider (Lucene, Azure, Algolia) should be registered by
        // provider-specific packages that extend this service
        services.AddScoped<ISearchService, SearchService>();

        // Add MVC controllers for search API
        services.AddControllers()
            .AddApplicationPart(typeof(BaselineSearchServiceCollectionExtensions).Assembly);

        return services;
    }

    /// <summary>
    /// Adds a custom search provider implementation.
    /// </summary>
    /// <typeparam name="TService">The search service implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSearchProvider<TService>(
        this IServiceCollection services) where TService : class, ISearchService
    {
        // Remove default search service registration and replace with custom
        services.AddScoped<ISearchService, TService>();
        return services;
    }

    /// <summary>
    /// Adds a custom index service implementation.
    /// </summary>
    /// <typeparam name="TService">The index service implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSearchIndexProvider<TService>(
        this IServiceCollection services) where TService : class, ISearchIndexService
    {
        services.AddScoped<ISearchIndexService, TService>();
        return services;
    }
}
