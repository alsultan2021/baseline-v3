using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Helpers;

namespace Baseline.Ecommerce.Admin.Services;

/// <summary>
/// Service for retrieving the default content language.
/// This ensures consistent data retrieval across multilingual content scenarios.
/// </summary>
public interface IDefaultContentLanguageRetriever
{
    /// <summary>
    /// Retrieves the default content language with caching for performance.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The default ContentLanguageInfo.</returns>
    Task<ContentLanguageInfo> GetAsync(CancellationToken cancellationToken = default);
}

/// <inheritdoc />
public sealed class DefaultContentLanguageRetriever : IDefaultContentLanguageRetriever
{
    // Cache duration in minutes (24 hours)
    private const int ONE_DAY = 24 * 60;

    private readonly IInfoProvider<ContentLanguageInfo> contentLanguageInfoProvider;
    private readonly IProgressiveCache progressiveCache;
    private readonly ICacheDependencyBuilderFactory cacheDependencyBuilderFactory;

    public DefaultContentLanguageRetriever(
        IInfoProvider<ContentLanguageInfo> contentLanguageInfoProvider,
        IProgressiveCache progressiveCache,
        ICacheDependencyBuilderFactory cacheDependencyBuilderFactory)
    {
        this.contentLanguageInfoProvider = contentLanguageInfoProvider;
        this.progressiveCache = progressiveCache;
        this.cacheDependencyBuilderFactory = cacheDependencyBuilderFactory;
    }

    /// <inheritdoc />
    public async Task<ContentLanguageInfo> GetAsync(CancellationToken cancellationToken = default)
    {
        // Uses progressive cache to avoid repeated database queries
        return await progressiveCache.LoadAsync(async (cacheSettings, token) =>
        {
            // Sets up cache dependency to invalidate when languages change
            var cacheDependencyBuilder = cacheDependencyBuilderFactory.Create();
            cacheSettings.CacheDependency = cacheDependencyBuilder
                .ForInfoObjects<ContentLanguageInfo>()
                    .All()
                    .Builder()
                .Build();

            // Queries for the default language (marked as default in the system)
            var result = await contentLanguageInfoProvider.Get()
               .WhereTrue(nameof(ContentLanguageInfo.ContentLanguageIsDefault))
               .TopN(1)
               .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

            return result.FirstOrDefault()
                ?? throw new InvalidOperationException("No default content language is configured.");
        },
        // Caches for one day with automatic invalidation when content languages change
        new CacheSettings(ONE_DAY, true, nameof(DefaultContentLanguageRetriever), nameof(GetAsync)), cancellationToken);
    }
}
