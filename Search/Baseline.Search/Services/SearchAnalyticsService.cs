using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baseline.Search;

/// <summary>
/// Implementation of search analytics service with in-memory storage
/// backed by periodic JSON file persistence.
/// </summary>
public class SearchAnalyticsService : ISearchAnalyticsService, IDisposable
{
    private readonly BaselineSearchOptions _options;
    private readonly ILogger<SearchAnalyticsService> _logger;
    private readonly string _storagePath;
    private readonly Timer? _flushTimer;
    private bool _loaded;
    private bool _dirty;
    private readonly object _loadLock = new();

    // In-memory storage for analytics, flushed to disk periodically
    private static readonly ConcurrentDictionary<string, SearchTrackingData> SearchHistory = new();
    private static readonly ConcurrentDictionary<string, ClickTrackingData> ClickHistory = new();

    private const string SearchFileName = "search-history.json";
    private const string ClickFileName = "click-history.json";
    private const int MaxEntries = 10_000;
    private const int EvictionBatch = 1_000;

    public SearchAnalyticsService(
        IOptions<BaselineSearchOptions> options,
        ILogger<SearchAnalyticsService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _storagePath = _options.AnalyticsStoragePath;

        if (_options.EnableAnalytics)
        {
            var interval = TimeSpan.FromMinutes(
                Math.Max(1, _options.AnalyticsFlushIntervalMinutes));
            _flushTimer = new Timer(_ => FlushToDisk(), null, interval, interval);
        }
    }

    /// <inheritdoc />
    public Task TrackSearchAsync(SearchTrackingData data)
    {
        if (!_options.EnableAnalytics)
        {
            return Task.CompletedTask;
        }

        EnsureLoaded();

        try
        {
            data.Timestamp = DateTimeOffset.UtcNow;
            SearchHistory.TryAdd(data.SearchId, data);
            _dirty = true;

            // Cleanup old entries (keep last MaxEntries)
            if (SearchHistory.Count > MaxEntries)
            {
                var oldestKeys = SearchHistory
                    .OrderBy(x => x.Value.Timestamp)
                    .Take(EvictionBatch)
                    .Select(x => x.Key)
                    .ToList();

                foreach (var key in oldestKeys)
                {
                    SearchHistory.TryRemove(key, out _);
                }
            }

            _logger.LogDebug("SearchAnalytics: Tracked search '{Query}' with {Results} results",
                data.Query, data.ResultCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SearchAnalytics: Error tracking search");
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task TrackClickAsync(string searchId, string documentId, int position)
    {
        if (!_options.EnableAnalytics)
        {
            return Task.CompletedTask;
        }

        EnsureLoaded();

        try
        {
            var clickData = new ClickTrackingData
            {
                SearchId = searchId,
                DocumentId = documentId,
                Position = position,
                Timestamp = DateTimeOffset.UtcNow
            };

            var key = $"{searchId}_{documentId}";
            ClickHistory.TryAdd(key, clickData);
            _dirty = true;

            _logger.LogDebug("SearchAnalytics: Tracked click on document '{Document}' at position {Position}",
                documentId, position);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SearchAnalytics: Error tracking click");
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IEnumerable<PopularSearch>> GetPopularSearchesAsync(int limit = 10, int days = 30)
    {
        EnsureLoaded();

        var cutoff = DateTimeOffset.UtcNow.AddDays(-days);

        var popularSearches = SearchHistory.Values
            .Where(s => s.Timestamp >= cutoff && !string.IsNullOrWhiteSpace(s.Query))
            .GroupBy(s => s.Query.ToLowerInvariant().Trim())
            .Select(g => new PopularSearch
            {
                Query = g.First().Query,
                Count = g.Count(),
                AverageResults = g.Average(s => s.ResultCount),
                LastSearched = g.Max(s => s.Timestamp)
            })
            .OrderByDescending(p => p.Count)
            .Take(limit);

        return Task.FromResult(popularSearches);
    }

    /// <inheritdoc />
    public Task<IEnumerable<FailedSearch>> GetFailedSearchesAsync(int limit = 50, int days = 30)
    {
        EnsureLoaded();

        var cutoff = DateTimeOffset.UtcNow.AddDays(-days);

        var failedSearches = SearchHistory.Values
            .Where(s => s.Timestamp >= cutoff && s.ResultCount == 0 && !string.IsNullOrWhiteSpace(s.Query))
            .GroupBy(s => s.Query.ToLowerInvariant().Trim())
            .Select(g => new FailedSearch
            {
                Query = g.First().Query,
                Count = g.Count(),
                LastSearched = g.Max(s => s.Timestamp)
            })
            .OrderByDescending(f => f.Count)
            .Take(limit);

        return Task.FromResult(failedSearches);
    }

    /// <inheritdoc />
    public Task<SearchAnalyticsSummary> GetSummaryAsync(DateTimeOffset from, DateTimeOffset to)
    {
        EnsureLoaded();

        var searches = SearchHistory.Values
            .Where(s => s.Timestamp >= from && s.Timestamp <= to)
            .ToList();

        var clicks = ClickHistory.Values
            .Where(c => c.Timestamp >= from && c.Timestamp <= to)
            .ToList();

        var summary = new SearchAnalyticsSummary
        {
            FromDate = from,
            ToDate = to,
            TotalSearches = searches.Count,
            TotalClicks = clicks.Count,
            UniqueQueries = searches.Select(s => s.Query.ToLowerInvariant().Trim()).Distinct().Count(),
            ZeroResultSearches = searches.Count(s => s.ResultCount == 0),
            AverageResultsPerSearch = searches.Count > 0 ? searches.Average(s => s.ResultCount) : 0,
            ClickThroughRate = searches.Count > 0 ? (double)clicks.Count / searches.Count : 0,
            AverageClickPosition = clicks.Count > 0 ? clicks.Average(c => c.Position) : 0
        };

        return Task.FromResult(summary);
    }

    /// <summary>
    /// Loads persisted data from disk on first access.
    /// </summary>
    private void EnsureLoaded()
    {
        if (_loaded)
        {
            return;
        }

        lock (_loadLock)
        {
            if (_loaded)
            {
                return;
            }

            LoadFromDisk();
            _loaded = true;
        }
    }

    private void LoadFromDisk()
    {
        try
        {
            var searchFile = System.IO.Path.Combine(_storagePath, SearchFileName);
            if (System.IO.File.Exists(searchFile))
            {
                var json = System.IO.File.ReadAllText(searchFile);
                var entries = JsonSerializer.Deserialize<Dictionary<string, SearchTrackingData>>(json);
                if (entries is not null)
                {
                    foreach (var kvp in entries)
                    {
                        SearchHistory.TryAdd(kvp.Key, kvp.Value);
                    }
                }

                _logger.LogInformation("SearchAnalytics: Loaded {Count} search entries from disk", SearchHistory.Count);
            }

            var clickFile = System.IO.Path.Combine(_storagePath, ClickFileName);
            if (System.IO.File.Exists(clickFile))
            {
                var json = System.IO.File.ReadAllText(clickFile);
                var entries = JsonSerializer.Deserialize<Dictionary<string, ClickTrackingData>>(json);
                if (entries is not null)
                {
                    foreach (var kvp in entries)
                    {
                        ClickHistory.TryAdd(kvp.Key, kvp.Value);
                    }
                }

                _logger.LogInformation("SearchAnalytics: Loaded {Count} click entries from disk", ClickHistory.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SearchAnalytics: Failed to load persisted data, starting fresh");
        }
    }

    private void FlushToDisk()
    {
        if (!_dirty)
        {
            return;
        }

        try
        {
            System.IO.Directory.CreateDirectory(_storagePath);

            var searchJson = JsonSerializer.Serialize(
                SearchHistory.ToDictionary(k => k.Key, v => v.Value),
                new JsonSerializerOptions { WriteIndented = false });
            System.IO.File.WriteAllText(System.IO.Path.Combine(_storagePath, SearchFileName), searchJson);

            var clickJson = JsonSerializer.Serialize(
                ClickHistory.ToDictionary(k => k.Key, v => v.Value),
                new JsonSerializerOptions { WriteIndented = false });
            System.IO.File.WriteAllText(System.IO.Path.Combine(_storagePath, ClickFileName), clickJson);

            _dirty = false;
            _logger.LogDebug("SearchAnalytics: Flushed {Searches} searches, {Clicks} clicks to disk",
                SearchHistory.Count, ClickHistory.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SearchAnalytics: Failed to flush data to disk");
        }
    }

    public void Dispose()
    {
        _flushTimer?.Dispose();
        FlushToDisk();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Internal click tracking data.
/// </summary>
public class ClickTrackingData
{
    public required string SearchId { get; init; }
    public required string DocumentId { get; init; }
    public required int Position { get; init; }
    public DateTimeOffset Timestamp { get; init; }
}
