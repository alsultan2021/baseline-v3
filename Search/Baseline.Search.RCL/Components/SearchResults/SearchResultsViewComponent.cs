using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Baseline.Search.Components;

/// <summary>
/// Renders search results.
/// </summary>
public class SearchResultsViewComponent(
    ISearchService searchService,
    ILogger<SearchResultsViewComponent> logger) : ViewComponent
{
    /// <summary>
    /// Renders search results for the given query.
    /// </summary>
    /// <param name="query">Search query.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Results per page.</param>
    /// <param name="indexName">Optional index name.</param>
    public async Task<IViewComponentResult> InvokeAsync(
        string query,
        int page = 1,
        int pageSize = 10,
        string? indexName = null)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return View(new SearchResultsViewModel
            {
                Query = query,
                Results = [],
                TotalCount = 0,
                CurrentPage = 1,
                TotalPages = 0,
                PageSize = pageSize
            });
        }

        try
        {
            var results = await searchService.SearchAsync(new SearchRequest
            {
                Query = query,
                Page = page,
                PageSize = pageSize,
                IndexName = indexName
            });

            var model = new SearchResultsViewModel
            {
                Query = query,
                Results = results.Items.AsReadOnly(),
                TotalCount = results.TotalCount,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)results.TotalCount / pageSize),
                PageSize = pageSize
            };

            return View(model);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SearchResultsViewComponent: Error executing search for query '{Query}'", query);

            return View(new SearchResultsViewModel
            {
                Query = query,
                Results = [],
                TotalCount = 0,
                CurrentPage = 1,
                TotalPages = 0,
                PageSize = pageSize,
                ErrorMessage = "Search is temporarily unavailable. Please try again later."
            });
        }
    }
}

/// <summary>
/// View model for search results.
/// </summary>
public class SearchResultsViewModel
{
    public string? Query { get; set; }
    public IReadOnlyList<SearchResult> Results { get; set; } = [];
    public int TotalCount { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; }
    public int PageSize { get; set; } = 10;
    public string? ErrorMessage { get; set; }
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
}
