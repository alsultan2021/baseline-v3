using System.Collections.Concurrent;
using CMS.Websites.Routing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baseline.Navigation;

/// <summary>
/// Service for caching navigation structures with proper invalidation support.
/// </summary>
public interface INavigationCacheService
{
    /// <summary>
    /// Gets or creates a cached navigation item.
    /// </summary>
    Task<T?> GetOrCreateAsync<T>(
        string cacheKey,
        Func<Task<T?>> factory,
        TimeSpan? absoluteExpiration = null,
        TimeSpan? slidingExpiration = null) where T : class;

    /// <summary>
    /// Invalidates a specific cache entry.
    /// </summary>
    void Invalidate(string cacheKey);

    /// <summary>
    /// Invalidates all navigation cache entries for a channel.
    /// </summary>
    void InvalidateChannel(string channelName);

    /// <summary>
    /// Invalidates all navigation cache entries.
    /// </summary>
    void InvalidateAll();

    /// <summary>
    /// Gets cache statistics.
    /// </summary>
    NavigationCacheStatistics GetStatistics();
}

/// <summary>
/// Implementation of navigation cache service with memory caching.
/// </summary>
public sealed class NavigationCacheService(
    IMemoryCache memoryCache,
    IWebsiteChannelContext websiteChannelContext,
    IOptions<BaselineNavigationOptions> options,
    ILogger<NavigationCacheService> logger) : INavigationCacheService
{
    private readonly MenuOptions _menuOptions = options.Value.Menus;
    private readonly SitemapOptions _sitemapOptions = options.Value.Sitemap;

    private const string CacheKeyPrefix = "Baseline_Nav_";
    private static readonly ConcurrentDictionary<string, byte> ActiveCacheKeys = new();

    private static int _cacheHits;
    private static int _cacheMisses;

    /// <inheritdoc />
    public async Task<T?> GetOrCreateAsync<T>(
        string cacheKey,
        Func<Task<T?>> factory,
        TimeSpan? absoluteExpiration = null,
        TimeSpan? slidingExpiration = null) where T : class
    {
        var fullKey = GetFullCacheKey(cacheKey);

        if (memoryCache.TryGetValue(fullKey, out T? cachedValue))
        {
            Interlocked.Increment(ref _cacheHits);
            logger.LogDebug("NavigationCacheService: Cache hit for '{CacheKey}'", cacheKey);
            return cachedValue;
        }

        Interlocked.Increment(ref _cacheMisses);
        logger.LogDebug("NavigationCacheService: Cache miss for '{CacheKey}'", cacheKey);

        var value = await factory();

        if (value is not null)
        {
            var entryOptions = new MemoryCacheEntryOptions();

            if (absoluteExpiration.HasValue)
            {
                entryOptions.AbsoluteExpirationRelativeToNow = absoluteExpiration.Value;
            }
            else
            {
                entryOptions.AbsoluteExpirationRelativeToNow =
                    TimeSpan.FromMinutes(_menuOptions.CacheDurationMinutes);
            }

            if (slidingExpiration.HasValue)
            {
                entryOptions.SlidingExpiration = slidingExpiration.Value;
            }

            // Register for eviction notification
            entryOptions.RegisterPostEvictionCallback((key, val, reason, state) =>
            {
                ActiveCacheKeys.TryRemove(key.ToString() ?? string.Empty, out _);
                logger.LogDebug("NavigationCacheService: Cache entry evicted '{Key}' - Reason: {Reason}",
                    key, reason);
            });

            memoryCache.Set(fullKey, value, entryOptions);
            ActiveCacheKeys.TryAdd(fullKey, 0);

            logger.LogDebug("NavigationCacheService: Cached '{CacheKey}'", cacheKey);
        }

        return value;
    }

    /// <inheritdoc />
    public void Invalidate(string cacheKey)
    {
        var fullKey = GetFullCacheKey(cacheKey);
        memoryCache.Remove(fullKey);
        ActiveCacheKeys.TryRemove(fullKey, out _);

        logger.LogInformation("NavigationCacheService: Invalidated cache '{CacheKey}'", cacheKey);
    }

    /// <inheritdoc />
    public void InvalidateChannel(string channelName)
    {
        var prefix = $"{CacheKeyPrefix}{channelName}_";
        var keysToRemove = ActiveCacheKeys.Keys
            .Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var key in keysToRemove)
        {
            memoryCache.Remove(key);
            ActiveCacheKeys.TryRemove(key, out _);
        }

        logger.LogInformation("NavigationCacheService: Invalidated {Count} cache entries for channel '{Channel}'",
            keysToRemove.Count, channelName);
    }

    /// <inheritdoc />
    public void InvalidateAll()
    {
        var keysToRemove = ActiveCacheKeys.Keys.ToList();

        foreach (var key in keysToRemove)
        {
            memoryCache.Remove(key);
            ActiveCacheKeys.TryRemove(key, out _);
        }

        logger.LogInformation("NavigationCacheService: Invalidated all {Count} cache entries",
            keysToRemove.Count);
    }

    /// <inheritdoc />
    public NavigationCacheStatistics GetStatistics()
    {
        var keyCount = ActiveCacheKeys.Count;

        return new NavigationCacheStatistics
        {
            CacheHits = _cacheHits,
            CacheMisses = _cacheMisses,
            ActiveEntries = keyCount,
            HitRatio = _cacheHits + _cacheMisses > 0
                ? (double)_cacheHits / (_cacheHits + _cacheMisses)
                : 0
        };
    }

    private string GetFullCacheKey(string cacheKey)
    {
        var channelName = websiteChannelContext.WebsiteChannelName ?? "default";
        return $"{CacheKeyPrefix}{channelName}_{cacheKey}";
    }
}

/// <summary>
/// Cache statistics for navigation caching.
/// </summary>
public record NavigationCacheStatistics
{
    /// <summary>
    /// Number of cache hits.
    /// </summary>
    public int CacheHits { get; init; }

    /// <summary>
    /// Number of cache misses.
    /// </summary>
    public int CacheMisses { get; init; }

    /// <summary>
    /// Number of active cache entries.
    /// </summary>
    public int ActiveEntries { get; init; }

    /// <summary>
    /// Cache hit ratio (0.0 to 1.0).
    /// </summary>
    public double HitRatio { get; init; }
}
