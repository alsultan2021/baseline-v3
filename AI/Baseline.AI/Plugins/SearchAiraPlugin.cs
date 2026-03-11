using System.ComponentModel;
using System.Text;

using Baseline.Search;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace Baseline.AI.Plugins;

/// <summary>
/// AIRA plugin integrating Baseline.Search capabilities — full-text, semantic,
/// faceted search, analytics, and suggestions — into the AIRA chat.
/// </summary>
[Description("Searches site content using the Baseline Search engine (keyword, semantic, faceted) and exposes search analytics.")]
public sealed class SearchAiraPlugin(
    IServiceProvider serviceProvider,
    ILogger<SearchAiraPlugin> logger) : IAiraPlugin
{
    /// <inheritdoc />
    public string PluginName => "Search";

    // ──────────────────────────────────────────────────────────────
    //  Full-text search
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Searches site content using the Baseline Search engine (Lucene/Azure).
    /// </summary>
    [KernelFunction("search_site")]
    [Description("Searches the site using the full-text search engine. Returns ranked results " +
                 "with title, URL, score, and highlighted excerpts. Supports filtering by " +
                 "content type and language.")]
    public async Task<string> SearchSiteAsync(
        [Description("Search query text")] string query,
        [Description("Content types to filter (comma-separated, e.g. 'ChevalRoyal.BlogPostPage,ChevalRoyal.ProductPage'). Empty = all.")] string? contentTypes = null,
        [Description("Language code (e.g. en, fr). Empty = default.")] string? language = null,
        [Description("Page number (1-based, default: 1)")] int page = 1,
        [Description("Results per page (default: 10, max: 50)")] int pageSize = 10)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var searchService = scope.ServiceProvider.GetService<ISearchService>();
            if (searchService is null)
            {
                return "Error: Search service is not registered. Ensure Baseline.Search is configured.";
            }

            var request = new SearchRequest
            {
                Query = query,
                Page = Math.Max(1, page),
                PageSize = Math.Clamp(pageSize, 1, 50),
                Language = language,
                EnableHighlighting = true,
                EnableFuzzyMatching = true
            };

            if (!string.IsNullOrWhiteSpace(contentTypes))
            {
                request.ContentTypes = contentTypes
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToList();
            }

            var results = await searchService.SearchAsync(request);

            var sb = new StringBuilder();
            sb.AppendLine($"## Search Results for \"{query}\"");
            sb.AppendLine($"**{results.TotalCount}** results found (page {results.Page}/{results.TotalPages}, {results.ExecutionTimeMs}ms)");

            if (results.WasCorrected)
            {
                sb.AppendLine($"_Did you mean: **{results.CorrectedQuery}**?_");
            }

            sb.AppendLine();

            if (!results.HasResults)
            {
                sb.AppendLine("No results found. Try different keywords or broader filters.");
                return sb.ToString();
            }

            int rank = (results.Page - 1) * results.PageSize;
            foreach (var item in results.Items)
            {
                rank++;
                sb.AppendLine($"### {rank}. {item.Title}");
                sb.AppendLine($"- **URL**: {item.Url}");
                sb.AppendLine($"- **Type**: {item.ContentType}");
                sb.AppendLine($"- **Score**: {item.Score:F3}");

                if (!string.IsNullOrWhiteSpace(item.Excerpt))
                {
                    sb.AppendLine($"- **Excerpt**: {item.Excerpt}");
                }

                sb.AppendLine();
            }

            // Facets
            if (results.Facets.Count > 0)
            {
                sb.AppendLine("### Available Filters");
                foreach (var facet in results.Facets)
                {
                    sb.AppendLine($"- **{facet.FieldName}**: {string.Join(", ", facet.Values.Take(10).Select(v => $"{v.Value} ({v.Count})"))}");
                }
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "SearchAira: search_site failed for '{Query}'", query);
            return $"Error searching: {ex.Message}";
        }
    }

    // ──────────────────────────────────────────────────────────────
    //  Semantic / AI search
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Performs a semantic (AI-powered) search over site content.
    /// </summary>
    [KernelFunction("semantic_search")]
    [Description("Performs semantic (AI-powered) search — finds results by meaning rather than " +
                 "exact keywords. Uses embeddings for similarity matching. Good for natural " +
                 "language queries like 'articles about healthy eating'.")]
    public async Task<string> SemanticSearchAsync(
        [Description("Natural language query")] string query,
        [Description("Maximum results (default: 10)")] int topK = 10)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var semanticSearch = scope.ServiceProvider.GetService<ISemanticSearchService>();
            if (semanticSearch is null)
            {
                return "Error: Semantic search is not configured. Ensure an embedding provider is registered.";
            }

            int max = Math.Clamp(topK, 1, 50);
            var results = await semanticSearch.SemanticSearchAsync(query, max);

            var sb = new StringBuilder();
            sb.AppendLine($"## Semantic Search: \"{query}\"");
            sb.AppendLine($"**{results.TotalCount}** results by meaning similarity");
            sb.AppendLine();

            int rank = 0;
            foreach (var item in results.Items)
            {
                rank++;
                sb.AppendLine($"{rank}. **{item.Title}** (similarity: {item.Score:P1})");

                if (!string.IsNullOrWhiteSpace(item.Url))
                {
                    sb.AppendLine($"   URL: {item.Url}");
                }

                if (!string.IsNullOrWhiteSpace(item.Excerpt))
                {
                    sb.AppendLine($"   {item.Excerpt}");
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "SearchAira: semantic_search failed for '{Query}'", query);
            return $"Error: {ex.Message}";
        }
    }

    // ──────────────────────────────────────────────────────────────
    //  Similar content
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Finds content similar to a given document.
    /// </summary>
    [KernelFunction("find_similar_content")]
    [Description("Finds content items similar to a given document ID. " +
                 "Useful for content recommendations and gap analysis.")]
    public async Task<string> FindSimilarContentAsync(
        [Description("Document ID to find similar content for")] string documentId,
        [Description("Maximum results (default: 5)")] int limit = 5)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var semanticSearch = scope.ServiceProvider.GetService<ISemanticSearchService>();
            if (semanticSearch is null)
            {
                return "Error: Semantic search is not configured.";
            }

            var results = await semanticSearch.GetSimilarContentAsync(documentId, Math.Clamp(limit, 1, 20));
            var items = results.ToList();

            if (items.Count == 0)
            {
                return $"No similar content found for document '{documentId}'.";
            }

            var sb = new StringBuilder();
            sb.AppendLine($"## Content Similar to '{documentId}'");
            sb.AppendLine();

            foreach (var item in items)
            {
                sb.AppendLine($"- **{item.Title}** (similarity: {item.Score:P1}) — {item.Url}");
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "SearchAira: find_similar failed for '{DocumentId}'", documentId);
            return $"Error: {ex.Message}";
        }
    }

    // ──────────────────────────────────────────────────────────────
    //  Suggestions / Autocomplete
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Gets search autocomplete suggestions.
    /// </summary>
    [KernelFunction("search_suggestions")]
    [Description("Gets search autocomplete suggestions for a partial query. " +
                 "Useful for understanding what users commonly search for.")]
    public async Task<string> GetSuggestionsAsync(
        [Description("Partial query text")] string query,
        [Description("Maximum suggestions (default: 10)")] int limit = 10)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var searchService = scope.ServiceProvider.GetService<ISearchService>();
            if (searchService is null)
            {
                return "Error: Search service is not registered.";
            }

            var suggestions = await searchService.GetSuggestionsAsync(query, Math.Clamp(limit, 1, 20));
            var items = suggestions.ToList();

            if (items.Count == 0)
            {
                return $"No suggestions for '{query}'.";
            }

            var sb = new StringBuilder();
            sb.AppendLine($"## Suggestions for \"{query}\"");

            foreach (var s in items)
            {
                sb.AppendLine($"- {s.Text} (score: {s.Score:F2})");
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "SearchAira: suggestions failed for '{Query}'", query);
            return $"Error: {ex.Message}";
        }
    }

    // ──────────────────────────────────────────────────────────────
    //  Search Analytics
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Gets search analytics — popular searches, zero-result queries, and usage summary.
    /// </summary>
    [KernelFunction("search_analytics")]
    [Description("Gets search analytics including popular searches, failed (zero-result) searches, " +
                 "and a usage summary. Helps identify content gaps and user intent.")]
    public async Task<string> GetSearchAnalyticsAsync(
        [Description("Number of days to analyze (default: 30)")] int days = 30,
        [Description("Max items per category (default: 10)")] int limit = 10)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var analytics = scope.ServiceProvider.GetService<ISearchAnalyticsService>();
            if (analytics is null)
            {
                return "Error: Search analytics is not configured.";
            }

            int max = Math.Clamp(limit, 1, 50);
            int dayRange = Math.Clamp(days, 1, 365);

            var popularTask = analytics.GetPopularSearchesAsync(max, dayRange);
            var failedTask = analytics.GetFailedSearchesAsync(max, dayRange);
            var summaryTask = analytics.GetSummaryAsync(
                DateTimeOffset.UtcNow.AddDays(-dayRange), DateTimeOffset.UtcNow);

            await Task.WhenAll(popularTask, failedTask, summaryTask);

            var popular = popularTask.Result.ToList();
            var failed = failedTask.Result.ToList();
            var summary = summaryTask.Result;

            var sb = new StringBuilder();
            sb.AppendLine($"## Search Analytics (Last {dayRange} Days)");
            sb.AppendLine();

            // Summary
            sb.AppendLine("### Usage Summary");
            sb.AppendLine($"- **Total searches**: {summary.TotalSearches:N0}");
            sb.AppendLine($"- **Unique queries**: {summary.UniqueQueries:N0}");
            sb.AppendLine($"- **Avg results/search**: {summary.AverageResultsPerSearch:F1}");
            sb.AppendLine($"- **Click-through rate**: {summary.OverallClickThroughRate:P1}");
            sb.AppendLine($"- **Zero-result rate**: {summary.ZeroResultsRate:P1}");
            sb.AppendLine();

            // Popular searches
            if (popular.Count > 0)
            {
                sb.AppendLine("### Popular Searches");
                foreach (var p in popular)
                {
                    sb.AppendLine($"- \"{p.Query}\" — {p.Count} searches");
                }

                sb.AppendLine();
            }

            // Failed searches (content gaps)
            if (failed.Count > 0)
            {
                sb.AppendLine("### Failed Searches (Content Gaps)");
                sb.AppendLine("_These queries return no results — consider creating content for them:_");

                foreach (var f in failed)
                {
                    sb.AppendLine($"- \"{f.Query}\" — {f.Count} attempts");
                }
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "SearchAira: search_analytics failed");
            return $"Error: {ex.Message}";
        }
    }

    // ──────────────────────────────────────────────────────────────
    //  Index management
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Gets search index statistics.
    /// </summary>
    [KernelFunction("search_index_stats")]
    [Description("Gets search index statistics: document count, index size, last rebuild time, " +
                 "field count, and health status.")]
    public async Task<string> GetIndexStatsAsync()
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var indexService = scope.ServiceProvider.GetService<ISearchIndexService>();
            if (indexService is null)
            {
                return "Error: Search index service is not registered.";
            }

            var stats = await indexService.GetStatisticsAsync();

            var sb = new StringBuilder();
            sb.AppendLine("## Search Index Statistics");
            sb.AppendLine($"- **Documents**: {stats.DocumentCount:N0}");
            sb.AppendLine($"- **Size**: {stats.IndexSizeBytes / 1024.0 / 1024.0:F2} MB");
            sb.AppendLine($"- **Last updated**: {stats.LastUpdated:yyyy-MM-dd HH:mm} UTC");
            if (stats.LastRebuild.HasValue)
            {
                sb.AppendLine($"- **Last rebuild**: {stats.LastRebuild:yyyy-MM-dd HH:mm} UTC");
            }

            if (stats.DocumentCountByType.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("### Documents by Type");
                foreach (var (type, count) in stats.DocumentCountByType)
                {
                    sb.AppendLine($"- {type}: {count:N0}");
                }
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "SearchAira: index_stats failed");
            return $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Triggers a full search index rebuild.
    /// </summary>
    [KernelFunction("rebuild_search_index")]
    [Description("Triggers a full search index rebuild. This re-indexes all content. " +
                 "May take several minutes for large sites. Use when content is missing from search.")]
    public async Task<string> RebuildIndexAsync()
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var indexService = scope.ServiceProvider.GetService<ISearchIndexService>();
            if (indexService is null)
            {
                return "Error: Search index service is not registered.";
            }

            logger.LogInformation("SearchAira: Triggering full index rebuild via AIRA");
            await indexService.RebuildIndexAsync();

            var stats = await indexService.GetStatisticsAsync();

            return $"Index rebuild completed. **{stats.DocumentCount:N0}** documents indexed.";
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "SearchAira: rebuild_index failed");
            return $"Error rebuilding index: {ex.Message}";
        }
    }
}
