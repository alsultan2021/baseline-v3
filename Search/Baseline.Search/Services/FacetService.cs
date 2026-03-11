using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baseline.Search;

/// <summary>
/// Implementation of facet service with caching.
/// </summary>
public class FacetService(
    IMemoryCache memoryCache,
    IOptions<BaselineSearchOptions> options,
    ILogger<FacetService> logger) : IFacetService
{
    private readonly BaselineSearchOptions _options = options.Value;
    private const string CacheKeyPrefix = "Baseline_SearchFacets_";

    /// <inheritdoc />
    public virtual async Task<IEnumerable<SearchFacet>> GetFacetsAsync(SearchRequest request)
    {
        var cacheKey = $"{CacheKeyPrefix}{GenerateFacetCacheKey(request)}";

        if (memoryCache.TryGetValue(cacheKey, out IEnumerable<SearchFacet>? cachedFacets))
        {
            logger.LogDebug("FacetService: Returning cached facets");
            return cachedFacets ?? [];
        }

        var facets = await ComputeFacetsAsync(request);

        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
            SlidingExpiration = TimeSpan.FromMinutes(2),
            Size = 1
        };

        memoryCache.Set(cacheKey, facets, cacheOptions);

        return facets;
    }

    /// <inheritdoc />
    public virtual async Task<IEnumerable<FacetValue>> GetFacetValuesAsync(string fieldName, int limit = 100)
    {
        var cacheKey = $"{CacheKeyPrefix}Values_{fieldName}_{limit}";

        if (memoryCache.TryGetValue(cacheKey, out IEnumerable<FacetValue>? cachedValues))
        {
            return cachedValues ?? [];
        }

        var values = await ComputeFacetValuesAsync(fieldName, limit);

        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
            Size = 1
        };

        memoryCache.Set(cacheKey, values, cacheOptions);

        return values;
    }

    /// <summary>
    /// Computes facets for a search request. Override in provider-specific implementation.
    /// </summary>
    protected virtual Task<IEnumerable<SearchFacet>> ComputeFacetsAsync(SearchRequest request)
    {
        // Default implementation returns common facets
        var facets = new List<SearchFacet>
        {
            new()
            {
                FieldName = "contentType",
                DisplayName = "Content Type",
                Type = FacetType.Terms,
                Values = []
            },
            new()
            {
                FieldName = "category",
                DisplayName = "Category",
                Type = FacetType.Terms,
                Values = []
            }
        };

        return Task.FromResult<IEnumerable<SearchFacet>>(facets);
    }

    /// <summary>
    /// Computes facet values for a field. Override in provider-specific implementation.
    /// </summary>
    protected virtual Task<IEnumerable<FacetValue>> ComputeFacetValuesAsync(string fieldName, int limit)
    {
        return Task.FromResult<IEnumerable<FacetValue>>([]);
    }

    private static string GenerateFacetCacheKey(SearchRequest request)
    {
        var contentTypes = string.Join(",", request.ContentTypes);
        var filters = string.Join(",", request.FacetFilters.SelectMany(f => f.Value.Select(v => $"{f.Key}:{v}")));
        return $"{request.IndexName}_{contentTypes}_{filters}_{request.Language}";
    }
}
