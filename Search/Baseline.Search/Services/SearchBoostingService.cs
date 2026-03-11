using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baseline.Search;

/// <summary>
/// Implementation of search boosting service.
/// </summary>
public class SearchBoostingService(
    IOptions<BaselineSearchOptions> options,
    ILogger<SearchBoostingService> logger) : ISearchBoostingService
{
    private readonly BaselineSearchOptions _options = options.Value;

    // Default boost factors (can be configured per project)
    private static readonly Dictionary<string, double> DefaultContentTypeBoosts = new()
    {
        { "Article", 1.5 },
        { "ProductPage", 1.3 },
        { "BlogPost", 1.2 },
        { "FAQ", 1.4 },
        { "LandingPage", 1.1 }
    };

    private static readonly Dictionary<string, double> DefaultFieldBoosts = new()
    {
        { "title", 3.0 },
        { "heading", 2.5 },
        { "keywords", 2.0 },
        { "description", 1.5 },
        { "content", 1.0 }
    };

    /// <inheritdoc />
    public Task<SearchBoostFactors> GetBoostFactorsAsync(SearchRequest request)
    {
        var factors = new SearchBoostFactors
        {
            ContentTypeBoosts = new Dictionary<string, double>(DefaultContentTypeBoosts),
            FieldBoosts = new Dictionary<string, double>(DefaultFieldBoosts),
            RecencyBoost = 1.0,
            PopularityBoost = 1.0
        };

        // Apply query-specific boost adjustments
        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            // Boost FAQ content for question-like queries
            if (request.Query.StartsWith("how", StringComparison.OrdinalIgnoreCase) ||
                request.Query.StartsWith("what", StringComparison.OrdinalIgnoreCase) ||
                request.Query.StartsWith("why", StringComparison.OrdinalIgnoreCase) ||
                request.Query.Contains('?'))
            {
                if (factors.ContentTypeBoosts.TryGetValue("FAQ", out var faqBoost))
                {
                    factors.ContentTypeBoosts["FAQ"] = faqBoost * 1.5;
                }
            }

            // Boost product pages for commercial intent queries
            if (request.Query.Contains("buy", StringComparison.OrdinalIgnoreCase) ||
                request.Query.Contains("price", StringComparison.OrdinalIgnoreCase) ||
                request.Query.Contains("order", StringComparison.OrdinalIgnoreCase))
            {
                if (factors.ContentTypeBoosts.TryGetValue("ProductPage", out var productBoost))
                {
                    factors.ContentTypeBoosts["ProductPage"] = productBoost * 1.5;
                }
            }
        }

        logger.LogDebug("SearchBoostingService: Generated boost factors for query '{Query}'", request.Query);

        return Task.FromResult(factors);
    }

    /// <inheritdoc />
    public Task<IEnumerable<SearchResult>> ApplyBoostingAsync(
        IEnumerable<SearchResult> results,
        SearchBoostFactors factors)
    {
        var resultsList = results.ToList();

        foreach (var result in resultsList)
        {
            var originalScore = result.Score;
            var boostedScore = originalScore;

            // Apply content type boost
            if (factors.ContentTypeBoosts.TryGetValue(result.ContentType, out var contentTypeBoost))
            {
                boostedScore *= contentTypeBoost;
            }

            // Apply recency boost (newer content gets higher boost)
            if (factors.RecencyBoost > 0 && result.LastModified.HasValue)
            {
                var daysSinceModified = (DateTimeOffset.UtcNow - result.LastModified.Value).TotalDays;
                var recencyFactor = Math.Max(0.5, 1.0 - (daysSinceModified / 365.0) * (1.0 - factors.RecencyBoost));
                boostedScore *= recencyFactor;
            }

            // Apply popularity boost (if available)
            if (factors.PopularityBoost > 0 && result.Metadata.TryGetValue("popularity", out var popularityValue))
            {
                if (double.TryParse(popularityValue?.ToString(), out var popularity))
                {
                    var popularityFactor = 1.0 + (popularity * factors.PopularityBoost * 0.1);
                    boostedScore *= popularityFactor;
                }
            }

            result.Score = boostedScore;

            logger.LogTrace("SearchBoostingService: Boosted '{Title}' from {Original} to {Boosted}",
                result.Title, originalScore, boostedScore);
        }

        // Re-sort by boosted score
        var sortedResults = resultsList.OrderByDescending(r => r.Score);

        return Task.FromResult<IEnumerable<SearchResult>>(sortedResults);
    }

    /// <summary>
    /// Configures custom content type boosts.
    /// </summary>
    public void ConfigureContentTypeBoost(string contentType, double boost)
    {
        DefaultContentTypeBoosts[contentType] = boost;
        logger.LogInformation("SearchBoostingService: Set content type boost for '{ContentType}' to {Boost}",
            contentType, boost);
    }

    /// <summary>
    /// Configures custom field boosts.
    /// </summary>
    public void ConfigureFieldBoost(string fieldName, double boost)
    {
        DefaultFieldBoosts[fieldName] = boost;
        logger.LogInformation("SearchBoostingService: Set field boost for '{Field}' to {Boost}",
            fieldName, boost);
    }
}
