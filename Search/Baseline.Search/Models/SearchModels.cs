namespace Baseline.Search;

/// <summary>
/// Search request parameters.
/// </summary>
public class SearchRequest
{
    /// <summary>
    /// The search query text.
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Page number (1-based).
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Number of results per page.
    /// </summary>
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// Optional search index name.
    /// </summary>
    public string? IndexName { get; set; }

    /// <summary>
    /// Content types to search.
    /// </summary>
    public IList<string> ContentTypes { get; set; } = [];

    /// <summary>
    /// Filter by specific fields.
    /// </summary>
    public Dictionary<string, object> Filters { get; set; } = [];

    /// <summary>
    /// Facet filters to apply.
    /// </summary>
    public Dictionary<string, IList<string>> FacetFilters { get; set; } = [];

    /// <summary>
    /// Fields to return in results.
    /// </summary>
    public IList<string> Fields { get; set; } = [];

    /// <summary>
    /// Field to sort by.
    /// </summary>
    public string? SortBy { get; set; }

    /// <summary>
    /// Sort in descending order.
    /// </summary>
    public bool SortDescending { get; set; }

    /// <summary>
    /// Enable highlighting in results.
    /// </summary>
    public bool EnableHighlighting { get; set; } = true;

    /// <summary>
    /// Language code for language-specific search.
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// Enable fuzzy matching.
    /// </summary>
    public bool EnableFuzzyMatching { get; set; } = true;

    /// <summary>
    /// Culture code for the search.
    /// </summary>
    public string? Culture { get; set; }

    /// <summary>
    /// Specific facet fields to include in results.
    /// </summary>
    public IList<string> FacetFields { get; set; } = [];

    /// <summary>
    /// Whether to include default facets (e.g., ContentType) in results.
    /// </summary>
    public bool IncludeFacets { get; set; }
}

/// <summary>
/// Search results container.
/// </summary>
public class SearchResults
{
    /// <summary>
    /// Unique ID for this search.
    /// </summary>
    public string SearchId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// The original query.
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Search result items.
    /// </summary>
    public IList<SearchResult> Items { get; set; } = [];

    /// <summary>
    /// Total number of matching documents.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number.
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Page size.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of pages.
    /// </summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    /// <summary>
    /// Available facets.
    /// </summary>
    public IList<SearchFacet> Facets { get; set; } = [];

    /// <summary>
    /// Search execution time in milliseconds.
    /// </summary>
    public long ExecutionTimeMs { get; set; }

    /// <summary>
    /// Corrected query (if spell correction was applied).
    /// </summary>
    public string? CorrectedQuery { get; set; }

    /// <summary>
    /// Whether spell correction was applied.
    /// </summary>
    public bool WasCorrected => !string.IsNullOrEmpty(CorrectedQuery);

    public bool HasResults => Items.Count > 0;
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}

/// <summary>
/// Individual search result.
/// </summary>
public class SearchResult
{
    /// <summary>
    /// Document ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Result title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Result description/excerpt.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Primary excerpt for display. Returns first highlight or description.
    /// </summary>
    public string? Excerpt => Highlights.Count > 0 ? Highlights[0] : Description;

    /// <summary>
    /// URL to the content.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Content type.
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Relevance score.
    /// </summary>
    public double Score { get; set; }

    /// <summary>
    /// Highlighted excerpts.
    /// </summary>
    public IList<string> Highlights { get; set; } = [];

    /// <summary>
    /// Image URL if available.
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Last modified date.
    /// </summary>
    public DateTimeOffset? LastModified { get; set; }

    /// <summary>
    /// Additional fields from the index.
    /// </summary>
    public Dictionary<string, object> Fields { get; set; } = [];

    /// <summary>
    /// Additional metadata fields from the index.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = [];
}

/// <summary>
/// Facet type enumeration.
/// </summary>
public enum FacetType
{
    /// <summary>
    /// Terms/category facet.
    /// </summary>
    Terms,

    /// <summary>
    /// Range facet (numeric).
    /// </summary>
    Range,

    /// <summary>
    /// Date range facet.
    /// </summary>
    DateRange,

    /// <summary>
    /// Hierarchical facet.
    /// </summary>
    Hierarchical
}

/// <summary>
/// Search facet.
/// </summary>
public class SearchFacet
{
    /// <summary>
    /// Field name.
    /// </summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// Display name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Facet type.
    /// </summary>
    public FacetType Type { get; set; } = FacetType.Terms;

    /// <summary>
    /// Facet values.
    /// </summary>
    public IList<FacetValue> Values { get; set; } = [];
}

/// <summary>
/// Facet value.
/// </summary>
public class FacetValue
{
    /// <summary>
    /// Value.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Display label.
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Number of documents with this value.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Whether this value is currently selected.
    /// </summary>
    public bool IsSelected { get; set; }
}

/// <summary>
/// Search suggestion for autocomplete.
/// </summary>
public class SearchSuggestion
{
    /// <summary>
    /// Suggested query text.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Highlighted version of the suggestion.
    /// </summary>
    public string? HighlightedText { get; set; }

    /// <summary>
    /// Score/relevance.
    /// </summary>
    public double Score { get; set; }

    /// <summary>
    /// Optional URL for direct navigation.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Suggestion type (query, page, product, etc.).
    /// </summary>
    public string Type { get; set; } = "query";
}

/// <summary>
/// Document to be indexed.
/// </summary>
public class SearchDocument
{
    /// <summary>
    /// Unique document ID.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Document title.
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// Document content (for full-text search).
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// Document description/excerpt.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// URL to the document.
    /// </summary>
    public required string Url { get; set; }

    /// <summary>
    /// Content type.
    /// </summary>
    public required string ContentType { get; set; }

    /// <summary>
    /// Language code.
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// Culture code.
    /// </summary>
    public string? Culture { get; set; }

    /// <summary>
    /// Image URL.
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Last modified date.
    /// </summary>
    public DateTimeOffset LastModified { get; set; }

    /// <summary>
    /// Additional fields for indexing.
    /// </summary>
    public Dictionary<string, object> Fields { get; set; } = [];

    /// <summary>
    /// Categories/tags for faceting.
    /// </summary>
    public IList<string> Categories { get; set; } = [];
}

/// <summary>
/// Index rebuild progress.
/// </summary>
public record IndexRebuildProgress(int ProcessedCount, int TotalCount, string CurrentItem);

/// <summary>
/// Index statistics.
/// </summary>
public class IndexStatistics
{
    public int DocumentCount { get; set; }
    public long IndexSizeBytes { get; set; }
    public DateTimeOffset LastUpdated { get; set; }
    public DateTimeOffset? LastRebuild { get; set; }
    public Dictionary<string, int> DocumentCountByType { get; set; } = [];
}

/// <summary>
/// A single recorded index generation (rebuild snapshot).
/// </summary>
public class IndexGeneration
{
    public required string Id { get; set; }
    public required string IndexName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public int DocumentCount { get; set; }
    public long IndexSizeBytes { get; set; }
    public TimeSpan Duration { get; set; }
    public Dictionary<string, int> DocumentCountByType { get; set; } = [];
    public bool IsActive { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Index generation retention policy settings.
/// </summary>
public class IndexRetentionPolicy
{
    /// <summary>Max generations to keep per index. Default: 5</summary>
    public int MaxGenerations { get; set; } = 5;

    /// <summary>Max age of retained generations. Default: 30 days</summary>
    public TimeSpan MaxAge { get; set; } = TimeSpan.FromDays(30);
}

/// <summary>
/// Search tracking data.
/// </summary>
public class SearchTrackingData
{
    public required string SearchId { get; set; }
    public required string Query { get; set; }
    public int ResultCount { get; set; }
    public long ExecutionTimeMs { get; set; }
    public string? UserId { get; set; }
    public string? SessionId { get; set; }
    public Dictionary<string, string> Filters { get; set; } = [];
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Popular search query.
/// </summary>
public class PopularSearch
{
    public string Query { get; set; } = string.Empty;
    public int SearchCount { get; set; }
    public int ClickCount { get; set; }
    public int Count { get; set; }
    public double AverageResults { get; set; }
    public DateTimeOffset LastSearched { get; set; }
    public double ClickThroughRate => SearchCount > 0 ? (double)ClickCount / SearchCount : 0;
}

/// <summary>
/// Failed search (no results).
/// </summary>
public class FailedSearch
{
    public string Query { get; set; } = string.Empty;
    public int Count { get; set; }
    public DateTimeOffset LastSearched { get; set; }
}

/// <summary>
/// Search analytics summary.
/// </summary>
public class SearchAnalyticsSummary
{
    public DateTimeOffset FromDate { get; set; }
    public DateTimeOffset ToDate { get; set; }
    public int TotalSearches { get; set; }
    public int UniqueQueries { get; set; }
    public int TotalClicks { get; set; }
    public int ZeroResultSearches { get; set; }
    public double AverageResultsPerSearch { get; set; }
    public double ClickThroughRate { get; set; }
    public double AverageClickPosition { get; set; }
    public double OverallClickThroughRate { get; set; }
    public double ZeroResultsRate { get; set; }
    public double AverageExecutionTimeMs { get; set; }
    public IList<PopularSearch> TopSearches { get; set; } = [];
}
