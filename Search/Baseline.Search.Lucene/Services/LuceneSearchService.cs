using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baseline.Search.Lucene;

/// <summary>
/// Lucene-specific implementation of the search service.
/// </summary>
public class LuceneSearchService(
    ILuceneSearchRepository luceneRepository,
    ISearchIndexService indexService,
    IFacetService? facetService,
    ISearchBoostingService? boostingService,
    IMemberAuthorizationFilter? authorizationFilter,
    LuceneSearchOptions luceneOptions,
    IOptions<BaselineSearchOptions> options,
    ILogger<LuceneSearchService> logger) : SearchService(
        indexService, facetService, boostingService, authorizationFilter, options, logger)
{
    private readonly LuceneSearchOptions _luceneOptions = luceneOptions;

    /// <inheritdoc />
    protected override async Task<SearchResults> ExecuteSearchAsync(
        SearchRequest request,
        SearchBoostFactors? boostFactors)
    {
        try
        {
            // Build Lucene query
            var luceneQuery = new LuceneSearchQuery
            {
                Query = request.Query,
                IndexName = request.IndexName ?? _luceneOptions.DefaultIndexName,
                Page = request.Page,
                PageSize = request.PageSize,
                LanguageCode = request.Culture,
                IncludeSecuredContent = false
            };

            // Add content type filters
            if (request.Filters.TryGetValue("ContentTypes", out var contentTypesObj) &&
                contentTypesObj is string contentTypesStr)
            {
                luceneQuery.ContentTypes = contentTypesStr.Split(',').ToList();
            }

            // Enable faceted search for content type dimension
            if (request.FacetFields.Count > 0)
            {
                luceneQuery.FacetFields = [.. request.FacetFields];
            }
            else if (request.IncludeFacets)
            {
                luceneQuery.FacetFields = ["ContentType"];
            }

            // Execute search
            var luceneResults = await luceneRepository.SearchAsync(luceneQuery);

            // Map to SearchResults
            var results = new SearchResults
            {
                Query = request.Query,
                TotalCount = luceneResults.TotalCount,
                Page = luceneResults.Page,
                PageSize = luceneResults.PageSize,
                Items = luceneResults.Items.Select(MapToSearchResult).ToList()
            };

            return results;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "LuceneSearchService: Error searching for '{Query}'", request.Query);
            throw;
        }
    }

    /// <inheritdoc />
    protected override async Task<IEnumerable<SearchSuggestion>> ExecuteGetSuggestionsAsync(
        string query,
        int limit)
    {
        try
        {
            var suggestions = await luceneRepository.GetSuggestionsAsync(
                query,
                _luceneOptions.DefaultIndexName,
                limit);

            return suggestions.Select(s => new SearchSuggestion
            {
                Text = s,
                Score = 1.0
            });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "LuceneSearchService: Error getting suggestions for '{Query}'", query);
            return [];
        }
    }

    private static SearchResult MapToSearchResult(LuceneSearchResultItem item)
    {
        var result = new SearchResult
        {
            Id = item.ContentItemId.ToString(),
            Title = item.Title,
            Description = item.Description,
            Url = item.Url ?? string.Empty,
            ContentType = item.ContentType,
            Score = item.Score,
            Highlights = item.Highlights.Values.ToList(),
            ImageUrl = item.ThumbnailUrl,
            LastModified = item.Created
        };

        // Forward security metadata for authorization filtering
        foreach (var (key, value) in item.CustomFields)
        {
            if (value is not null)
            {
                result.Metadata[key] = value;
            }
        }

        return result;
    }
}
