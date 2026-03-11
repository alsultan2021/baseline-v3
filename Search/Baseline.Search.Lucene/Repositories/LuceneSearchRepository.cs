using Kentico.Xperience.Lucene.Core.Indexing;
using Kentico.Xperience.Lucene.Core.Search;
using Lucene.Net.Facet;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search.Highlight;
using LuceneSearch = global::Lucene.Net.Search;
using LuceneIndex = global::Lucene.Net.Index;
using LuceneDocuments = global::Lucene.Net.Documents;

namespace Baseline.Search.Lucene;

#region Search Repository

/// <summary>
/// Lucene-based search repository implementation.
/// </summary>
public interface ILuceneSearchRepository
{
    /// <summary>
    /// Searches the index with the given query.
    /// </summary>
    Task<LuceneSearchResults> SearchAsync(
        LuceneSearchQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets suggestions for autocomplete.
    /// </summary>
    Task<IReadOnlyList<string>> GetSuggestionsAsync(
        string prefix,
        string indexName,
        int maxResults = 10,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Lucene search query parameters.
/// </summary>
public sealed class LuceneSearchQuery
{
    /// <summary>
    /// The search text.
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Index name to search.
    /// </summary>
    public string IndexName { get; set; } = string.Empty;

    /// <summary>
    /// Page number (1-based).
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Results per page.
    /// </summary>
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// Fields to search in.
    /// </summary>
    public List<string> SearchFields { get; set; } = ["Title", "Description", "Content"];

    /// <summary>
    /// Filter by content types.
    /// </summary>
    public List<string> ContentTypes { get; set; } = [];

    /// <summary>
    /// Language code filter.
    /// </summary>
    public string? LanguageCode { get; set; }

    /// <summary>
    /// Whether to include secured content.
    /// </summary>
    public bool IncludeSecuredContent { get; set; }

    /// <summary>
    /// User's role tags for permission filtering.
    /// </summary>
    public List<string> UserRoleTags { get; set; } = [];

    /// <summary>
    /// Optional facet fields.
    /// </summary>
    public List<string> FacetFields { get; set; } = [];

    /// <summary>
    /// Sort field and direction.
    /// </summary>
    public LuceneSortOption? Sort { get; set; }
}

/// <summary>
/// Sort option for search results.
/// </summary>
public sealed class LuceneSortOption
{
    /// <summary>
    /// Field to sort by.
    /// </summary>
    public string Field { get; set; } = string.Empty;

    /// <summary>
    /// Sort direction.
    /// </summary>
    public LuceneSortDirection Direction { get; set; } = LuceneSortDirection.Ascending;
}

/// <summary>
/// Sort direction.
/// </summary>
public enum LuceneSortDirection
{
    Ascending,
    Descending
}

/// <summary>
/// Search results from Lucene.
/// </summary>
public sealed class LuceneSearchResults
{
    /// <summary>
    /// The search result items.
    /// </summary>
    public IReadOnlyList<LuceneSearchResultItem> Items { get; set; } = [];

    /// <summary>
    /// Total number of matching results.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number.
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Results per page.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Facet results if requested.
    /// </summary>
    public Dictionary<string, List<LuceneFacetValue>> Facets { get; set; } = [];

    /// <summary>
    /// Query execution time in milliseconds.
    /// </summary>
    public long QueryTimeMs { get; set; }

    /// <summary>
    /// Total number of pages.
    /// </summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
}

/// <summary>
/// Individual search result item.
/// </summary>
public sealed class LuceneSearchResultItem
{
    /// <summary>
    /// Content item ID.
    /// </summary>
    public int ContentItemId { get; set; }

    /// <summary>
    /// Title of the result.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Description or excerpt.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// URL to the content.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Thumbnail image URL.
    /// </summary>
    public string? ThumbnailUrl { get; set; }

    /// <summary>
    /// Content type name.
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Created date.
    /// </summary>
    public DateTimeOffset? Created { get; set; }

    /// <summary>
    /// Search relevance score.
    /// </summary>
    public float Score { get; set; }

    /// <summary>
    /// Highlighted matches in content.
    /// </summary>
    public Dictionary<string, string> Highlights { get; set; } = [];

    /// <summary>
    /// Additional custom fields.
    /// </summary>
    public Dictionary<string, object?> CustomFields { get; set; } = [];
}

/// <summary>
/// Facet value for aggregated results.
/// </summary>
public sealed class LuceneFacetValue
{
    /// <summary>
    /// The facet value.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Count of items with this value.
    /// </summary>
    public int Count { get; set; }
}

#endregion

#region Default Implementation

/// <summary>
/// Default implementation of Lucene search repository.
/// Uses Kentico.Xperience.Lucene.Core for search operations.
/// </summary>
public sealed class LuceneSearchRepository : ILuceneSearchRepository
{
    private readonly LuceneSearchOptions _options;
    private readonly ILuceneSearchCustomizations _customizations;
    private readonly ILuceneIndexManager? _indexManager;
    private readonly ILuceneSearchService? _luceneSearchService;

    public LuceneSearchRepository(
        LuceneSearchOptions options,
        ILuceneSearchCustomizations customizations,
        ILuceneIndexManager? indexManager = null,
        ILuceneSearchService? luceneSearchService = null)
    {
        _options = options;
        _customizations = customizations;
        _indexManager = indexManager;
        _luceneSearchService = luceneSearchService;
    }

    public async Task<LuceneSearchResults> SearchAsync(
        LuceneSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.UtcNow;

        // Apply customizations
        query = await _customizations.CustomizeQueryAsync(query, cancellationToken);

        // Check if Lucene services are available
        if (_indexManager is null || _luceneSearchService is null)
        {
            // Return empty results if Lucene is not configured
            var emptyResults = new LuceneSearchResults
            {
                Items = [],
                TotalCount = 0,
                Page = query.Page,
                PageSize = query.PageSize,
                QueryTimeMs = (DateTimeOffset.UtcNow - startTime).Milliseconds
            };
            return await _customizations.CustomizeResultsAsync(emptyResults, query, cancellationToken);
        }

        // Get the index
        var index = _indexManager.GetIndex(query.IndexName);
        if (index is null)
        {
            var emptyResults = new LuceneSearchResults
            {
                Items = [],
                TotalCount = 0,
                Page = query.Page,
                PageSize = query.PageSize,
                QueryTimeMs = (DateTimeOffset.UtcNow - startTime).Milliseconds
            };
            return await _customizations.CustomizeResultsAsync(emptyResults, query, cancellationToken);
        }

        // Build and execute search
        const int maxResults = 1000;
        var items = new List<LuceneSearchResultItem>();
        int totalHits = 0;
        var facets = new Dictionary<string, List<LuceneFacetValue>>();

        try
        {
            // Build search query for all specified fields
            var luceneQuery = BuildLuceneQuery(query, index.LuceneAnalyzer);

            // Use faceted search when facet fields are requested
            if (query.FacetFields.Count > 0 && _luceneSearchService is not null)
            {
                // Build a base query (text + language, no content type filter) for facet counting
                var facetBaseQuery = BuildFacetBaseQuery(query, index.LuceneAnalyzer);

                var (searchItems, hits, facetResults) = _luceneSearchService.UseSearcherWithFacets(
                    index,
                    facetBaseQuery,
                    maxResults,
                    (searcher, multiFacets) =>
                    {
                        var searchResults = searcher.Search(luceneQuery, maxResults);
                        var resultItems = new List<LuceneSearchResultItem>();

                        var offset = (query.Page - 1) * query.PageSize;
                        var scoreDocs = searchResults.ScoreDocs
                            .Skip(offset)
                            .Take(query.PageSize);

                        foreach (var scoreDoc in scoreDocs)
                        {
                            var doc = searcher.Doc(scoreDoc.Doc);
                            resultItems.Add(MapDocumentToSearchResult(doc, scoreDoc.Score));
                        }

                        // Extract facet counts
                        var extractedFacets = new Dictionary<string, List<LuceneFacetValue>>();
                        foreach (var facetField in query.FacetFields)
                        {
                            var topChildren = multiFacets.GetTopChildren(20, facetField);
                            if (topChildren?.LabelValues is not null)
                            {
                                extractedFacets[facetField] = topChildren.LabelValues
                                    .Select(lv => new LuceneFacetValue
                                    {
                                        Value = lv.Label,
                                        Count = (int)lv.Value
                                    })
                                    .ToList();
                            }
                        }

                        return (resultItems, searchResults.TotalHits, extractedFacets);
                    });

                items = searchItems;
                totalHits = hits;
                facets = facetResults;
            }
            else
            {
                // Non-faceted search path
                var (searchItems, hits) = _luceneSearchService!.UseSearcher(index, searcher =>
                {
                    var searchResults = searcher.Search(luceneQuery, maxResults);
                    var resultItems = new List<LuceneSearchResultItem>();

                    var offset = (query.Page - 1) * query.PageSize;
                    var scoreDocs = searchResults.ScoreDocs
                        .Skip(offset)
                        .Take(query.PageSize);

                    foreach (var scoreDoc in scoreDocs)
                    {
                        var doc = searcher.Doc(scoreDoc.Doc);
                        resultItems.Add(MapDocumentToSearchResult(doc, scoreDoc.Score));
                    }

                    return (resultItems, searchResults.TotalHits);
                });

                items = searchItems;
                totalHits = hits;
            }

            // Apply query-term highlighting on stored fields
            ApplyHighlights(items, luceneQuery, index.LuceneAnalyzer);
        }
        catch (Exception)
        {
            // Log and return empty on error
        }

        var results = new LuceneSearchResults
        {
            Items = items,
            TotalCount = totalHits,
            Page = query.Page,
            PageSize = query.PageSize,
            Facets = facets,
            QueryTimeMs = (DateTimeOffset.UtcNow - startTime).Milliseconds
        };

        return await _customizations.CustomizeResultsAsync(results, query, cancellationToken);
    }

    public Task<IReadOnlyList<string>> GetSuggestionsAsync(
        string prefix,
        string indexName,
        int maxResults = 10,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prefix) || _indexManager is null || _luceneSearchService is null)
        {
            return Task.FromResult<IReadOnlyList<string>>([]);
        }

        var index = _indexManager.GetIndex(indexName);
        if (index is null)
        {
            return Task.FromResult<IReadOnlyList<string>>([]);
        }

        var suggestions = new List<string>();

        try
        {
            // Use prefix query on Title field for suggestions
            var prefixQuery = new LuceneSearch.PrefixQuery(
                new LuceneIndex.Term("Title", prefix.ToLowerInvariant()));

            var foundSuggestions = _luceneSearchService.UseSearcher(index, searcher =>
            {
                var resultList = new List<string>();
                var searchResults = searcher.Search(prefixQuery, maxResults);

                foreach (var scoreDoc in searchResults.ScoreDocs)
                {
                    var doc = searcher.Doc(scoreDoc.Doc);
                    var title = doc.Get("Title");
                    if (!string.IsNullOrEmpty(title) && !resultList.Contains(title))
                    {
                        resultList.Add(title);
                    }
                }

                return resultList;
            });

            suggestions.AddRange(foundSuggestions);
        }
        catch
        {
            // Return empty on error
        }

        return Task.FromResult<IReadOnlyList<string>>(suggestions);
    }

    private static LuceneSearch.Query BuildLuceneQuery(
        LuceneSearchQuery query,
        global::Lucene.Net.Analysis.Analyzer analyzer)
    {
        if (string.IsNullOrWhiteSpace(query.Query))
        {
            return new LuceneSearch.MatchAllDocsQuery();
        }

        var boolQuery = new LuceneSearch.BooleanQuery();
        var queryText = query.Query.Trim();

        // Use MultiFieldQueryParser with field boosts for analyzer-aware search
        var boosts = new Dictionary<string, float>
        {
            ["Title"] = 2.0f,
            ["Description"] = 1.5f,
            ["Keywords"] = 1.8f,
            ["Content"] = 1.0f
        };

        // Only boost fields that are actually in the search fields list
        var activeBoosts = new Dictionary<string, float>();
        foreach (var field in query.SearchFields)
        {
            activeBoosts[field] = boosts.TryGetValue(field, out var boost) ? boost : 1.0f;
        }

        var parser = new MultiFieldQueryParser(
            global::Lucene.Net.Util.LuceneVersion.LUCENE_48,
            [.. query.SearchFields],
            analyzer,
            activeBoosts);
        parser.DefaultOperator = Operator.OR;
        parser.PhraseSlop = 3;

        var escaped = QueryParserBase.Escape(queryText);
        var parsedQuery = parser.Parse(escaped);
        boolQuery.Add(parsedQuery, LuceneSearch.Occur.MUST);

        // Add content type filter if specified
        if (query.ContentTypes.Count > 0)
        {
            var typeQuery = new LuceneSearch.BooleanQuery();
            foreach (var contentType in query.ContentTypes)
            {
                typeQuery.Add(
                    new LuceneSearch.TermQuery(
                        new LuceneIndex.Term("ContentType", contentType)),
                    LuceneSearch.Occur.SHOULD);
            }
            boolQuery.Add(typeQuery, LuceneSearch.Occur.MUST);
        }

        // Add language filter if specified
        if (!string.IsNullOrEmpty(query.LanguageCode))
        {
            boolQuery.Add(
                new LuceneSearch.TermQuery(
                    new LuceneIndex.Term("Language", query.LanguageCode)),
                LuceneSearch.Occur.MUST);
        }

        return boolQuery;
    }

    /// <summary>
    /// Builds a base query for facet counting (text + language, excludes content type filter).
    /// </summary>
    private static LuceneSearch.Query BuildFacetBaseQuery(
        LuceneSearchQuery query,
        global::Lucene.Net.Analysis.Analyzer analyzer)
    {
        if (string.IsNullOrWhiteSpace(query.Query))
        {
            var baseQuery = new LuceneSearch.BooleanQuery();

            if (!string.IsNullOrEmpty(query.LanguageCode))
            {
                baseQuery.Add(
                    new LuceneSearch.TermQuery(
                        new LuceneIndex.Term("Language", query.LanguageCode)),
                    LuceneSearch.Occur.MUST);
                return baseQuery;
            }

            return new LuceneSearch.MatchAllDocsQuery();
        }

        var boolQuery = new LuceneSearch.BooleanQuery();
        var queryText = query.Query.Trim();

        var boosts = new Dictionary<string, float>
        {
            ["Title"] = 2.0f,
            ["Description"] = 1.5f,
            ["Keywords"] = 1.8f,
            ["Content"] = 1.0f
        };

        var activeBoosts = new Dictionary<string, float>();
        foreach (var field in query.SearchFields)
        {
            activeBoosts[field] = boosts.TryGetValue(field, out var boost) ? boost : 1.0f;
        }

        var parser = new MultiFieldQueryParser(
            global::Lucene.Net.Util.LuceneVersion.LUCENE_48,
            [.. query.SearchFields],
            analyzer,
            activeBoosts);
        parser.DefaultOperator = Operator.OR;
        parser.PhraseSlop = 3;

        var escaped = QueryParserBase.Escape(queryText);
        var parsedQuery = parser.Parse(escaped);
        boolQuery.Add(parsedQuery, LuceneSearch.Occur.MUST);

        // Language filter only (no content type) so facet counts cover all content types
        if (!string.IsNullOrEmpty(query.LanguageCode))
        {
            boolQuery.Add(
                new LuceneSearch.TermQuery(
                    new LuceneIndex.Term("Language", query.LanguageCode)),
                LuceneSearch.Occur.MUST);
        }

        return boolQuery;
    }

    private static LuceneSearchResultItem MapDocumentToSearchResult(
        LuceneDocuments.Document doc,
        float score)
    {
        var item = new LuceneSearchResultItem
        {
            ContentItemId = int.TryParse(doc.Get("ContentItemID"), out var id) ? id : 0,
            Title = doc.Get("Title") ?? string.Empty,
            Description = doc.Get("Description"),
            Url = doc.Get("Url") ?? doc.Get("CanonicalUrl"),
            ThumbnailUrl = doc.Get("Thumbnail") ?? doc.Get("Image"),
            ContentType = doc.Get("ContentType") ?? string.Empty,
            Created = DateTime.TryParse(doc.Get("Created"), out var created) ? created : null,
            Score = score
        };

        // Propagate security metadata for authorization filtering
        var isSecured = doc.Get("IsSecured");
        if (!string.IsNullOrEmpty(isSecured))
        {
            item.CustomFields["IsSecured"] = isSecured;
        }

        var roleTags = doc.Get("MemberPermissionRoleTags");
        if (!string.IsNullOrEmpty(roleTags))
        {
            item.CustomFields["MemberPermissionRoleTags"] = roleTags;
        }

        return item;
    }

    /// <summary>
    /// Applies query-term highlighting to stored Title and Description fields.
    /// </summary>
    private static void ApplyHighlights(
        IList<LuceneSearchResultItem> items,
        LuceneSearch.Query luceneQuery,
        global::Lucene.Net.Analysis.Analyzer analyzer)
    {
        if (items.Count == 0)
        {
            return;
        }

        try
        {
            var formatter = new SimpleHTMLFormatter("<mark>", "</mark>");
            var scorer = new QueryScorer(luceneQuery);
            var highlighter = new Highlighter(formatter, scorer)
            {
                TextFragmenter = new SimpleFragmenter(150)
            };

            foreach (var item in items)
            {
                if (!string.IsNullOrEmpty(item.Title))
                {
                    var titleHighlight = highlighter.GetBestFragment(analyzer, "Title", item.Title);
                    if (!string.IsNullOrEmpty(titleHighlight))
                    {
                        item.Highlights["Title"] = titleHighlight;
                    }
                }

                if (!string.IsNullOrEmpty(item.Description))
                {
                    var descHighlight = highlighter.GetBestFragment(analyzer, "Description", item.Description);
                    if (!string.IsNullOrEmpty(descHighlight))
                    {
                        item.Highlights["Description"] = descHighlight;
                    }
                }
            }
        }
        catch
        {
            // Highlighting is non-critical — return results without highlights on error
        }
    }
}

#endregion
