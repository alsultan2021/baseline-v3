namespace Baseline.Search;

/// <summary>
/// Main search service interface.
/// </summary>
public interface ISearchService
{
    /// <summary>
    /// Performs a search query.
    /// </summary>
    Task<SearchResults> SearchAsync(SearchRequest request);

    /// <summary>
    /// Gets search suggestions for autocomplete.
    /// </summary>
    Task<IEnumerable<SearchSuggestion>> GetSuggestionsAsync(string query, int limit = 10);

    /// <summary>
    /// Gets related searches based on the current query.
    /// </summary>
    Task<IEnumerable<string>> GetRelatedSearchesAsync(string query, int limit = 5);
}

/// <summary>
/// Service for managing search indexes.
/// </summary>
public interface ISearchIndexService
{
    /// <summary>
    /// Indexes a content item.
    /// </summary>
    Task IndexAsync(SearchDocument document);

    /// <summary>
    /// Indexes multiple content items.
    /// </summary>
    Task IndexBatchAsync(IEnumerable<SearchDocument> documents);

    /// <summary>
    /// Removes a content item from the index.
    /// </summary>
    Task RemoveAsync(string documentId);

    /// <summary>
    /// Rebuilds the entire search index.
    /// </summary>
    Task RebuildIndexAsync(IProgress<IndexRebuildProgress>? progress = null);

    /// <summary>
    /// Gets index statistics.
    /// </summary>
    Task<IndexStatistics> GetStatisticsAsync();

    /// <summary>
    /// Clears the entire index.
    /// </summary>
    Task ClearIndexAsync();
}

/// <summary>
/// Service for faceted search.
/// </summary>
public interface IFacetService
{
    /// <summary>
    /// Gets available facets for a search query.
    /// </summary>
    Task<IEnumerable<SearchFacet>> GetFacetsAsync(SearchRequest request);

    /// <summary>
    /// Gets facet values for a specific field.
    /// </summary>
    Task<IEnumerable<FacetValue>> GetFacetValuesAsync(string fieldName, int limit = 100);
}

/// <summary>
/// Service for search analytics.
/// </summary>
public interface ISearchAnalyticsService
{
    /// <summary>
    /// Tracks a search query.
    /// </summary>
    Task TrackSearchAsync(SearchTrackingData data);

    /// <summary>
    /// Tracks a search result click.
    /// </summary>
    Task TrackClickAsync(string searchId, string documentId, int position);

    /// <summary>
    /// Gets popular searches.
    /// </summary>
    Task<IEnumerable<PopularSearch>> GetPopularSearchesAsync(int limit = 10, int days = 30);

    /// <summary>
    /// Gets searches with no results.
    /// </summary>
    Task<IEnumerable<FailedSearch>> GetFailedSearchesAsync(int limit = 50, int days = 30);

    /// <summary>
    /// Gets search analytics summary.
    /// </summary>
    Task<SearchAnalyticsSummary> GetSummaryAsync(DateTimeOffset from, DateTimeOffset to);
}

/// <summary>
/// Service for semantic/AI-powered search.
/// </summary>
public interface ISemanticSearchService
{
    /// <summary>
    /// Performs a semantic search using embeddings.
    /// </summary>
    Task<SearchResults> SemanticSearchAsync(string query, int topK = 10);

    /// <summary>
    /// Gets similar content items.
    /// </summary>
    Task<IEnumerable<SearchResult>> GetSimilarContentAsync(string documentId, int limit = 5);

    /// <summary>
    /// Generates embeddings for a text.
    /// </summary>
    Task<float[]> GenerateEmbeddingAsync(string text);

    /// <summary>
    /// Indexes document embeddings.
    /// </summary>
    Task IndexEmbeddingAsync(string documentId, float[] embedding);
}

/// <summary>
/// Service for member-based search result authorization.
/// </summary>
public interface IMemberAuthorizationFilter
{
    /// <summary>
    /// Filters search results based on member permissions.
    /// </summary>
    Task<IEnumerable<SearchResult>> FilterResultsAsync(IEnumerable<SearchResult> results);

    /// <summary>
    /// Checks if a member can access a specific document.
    /// </summary>
    Task<bool> CanAccessAsync(string documentId);

    /// <summary>
    /// Gets authorized content type codes for the current member.
    /// </summary>
    Task<IEnumerable<string>> GetAuthorizedContentTypesAsync();

    /// <summary>
    /// Gets authorized taxonomy tags for the current member.
    /// </summary>
    Task<IEnumerable<Guid>> GetAuthorizedTaxonomyTagsAsync();
}

/// <summary>
/// Service for search result boosting.
/// </summary>
public interface ISearchBoostingService
{
    /// <summary>
    /// Gets boost factors for a query.
    /// </summary>
    Task<SearchBoostFactors> GetBoostFactorsAsync(SearchRequest request);

    /// <summary>
    /// Applies boosting to search results.
    /// </summary>
    Task<IEnumerable<SearchResult>> ApplyBoostingAsync(IEnumerable<SearchResult> results, SearchBoostFactors factors);
}

/// <summary>
/// Service for tracking index generations and enforcing retention.
/// </summary>
public interface IIndexGenerationService
{
    /// <summary>Records a new generation after an index rebuild.</summary>
    Task<IndexGeneration> RecordGenerationAsync(string indexName, IndexStatistics stats, TimeSpan duration);

    /// <summary>Lists stored generations for an index, newest first.</summary>
    Task<IReadOnlyList<IndexGeneration>> GetGenerationsAsync(string indexName);

    /// <summary>Gets the currently active generation for an index.</summary>
    Task<IndexGeneration?> GetActiveGenerationAsync(string indexName);

    /// <summary>Applies the retention policy and removes expired generations.</summary>
    Task<int> ApplyRetentionPolicyAsync(string indexName, IndexRetentionPolicy? policy = null);
}

/// <summary>
/// Provider abstraction for generating text embeddings.
/// Implementations: AzureOpenAI, OpenAI, Pseudo (demo).
/// </summary>
public interface IEmbeddingProvider
{
    /// <summary>Generates an embedding vector for the given text.</summary>
    Task<float[]> GenerateEmbeddingAsync(string text);

    /// <summary>Generates embeddings for multiple texts in a single batch call.</summary>
    Task<IReadOnlyList<float[]>> GenerateBatchEmbeddingsAsync(IReadOnlyList<string> texts);
}

/// <summary>
/// Search boost factors.
/// </summary>
public class SearchBoostFactors
{
    /// <summary>
    /// Content type boost factors.
    /// </summary>
    public Dictionary<string, double> ContentTypeBoosts { get; set; } = [];

    /// <summary>
    /// Field boost factors.
    /// </summary>
    public Dictionary<string, double> FieldBoosts { get; set; } = [];

    /// <summary>
    /// Recency boost (higher = more boost for newer content).
    /// </summary>
    public double RecencyBoost { get; set; } = 1.0;

    /// <summary>
    /// Popularity boost factor.
    /// </summary>
    public double PopularityBoost { get; set; } = 1.0;
}
