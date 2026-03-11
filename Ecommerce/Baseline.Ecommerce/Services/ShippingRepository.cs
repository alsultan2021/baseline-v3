using CMS.Commerce;
using CMS.DataEngine;
using CMS.Helpers;
using CMS.Websites.Routing;

using ICacheDependencyBuilderFactory = CMS.Helpers.ICacheDependencyBuilderFactory;

namespace Baseline.Ecommerce.Services;

/// <summary>
/// Interface for shipping method repository operations.
/// </summary>
public interface IShippingRepository
{
    /// <summary>
    /// Returns a cached list of all enabled shipping methods.
    /// </summary>
    Task<IEnumerable<ShippingMethodInfo>> GetShippingMethodsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a shipping method by ID.
    /// </summary>
    Task<ShippingMethodInfo?> GetShippingByIdAsync(int shippingMethodId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository for managing shipping method information retrieval operations with caching.
/// </summary>
public sealed class ShippingRepository(
    IWebsiteChannelContext websiteChannelContext,
    IProgressiveCache cache,
    ICacheDependencyBuilderFactory cacheDependencyBuilderFactory,
    IInfoProvider<ShippingMethodInfo> shippingMethodInfoProvider) : IShippingRepository
{
    private const int CacheMinutes = 5;

    /// <inheritdoc />
    public async Task<IEnumerable<ShippingMethodInfo>> GetShippingMethodsAsync(CancellationToken cancellationToken = default)
    {
        if (websiteChannelContext.IsPreview)
        {
            return await GetShippingInternalAsync(cancellationToken);
        }

        var cacheSettings = new CacheSettings(CacheMinutes, websiteChannelContext.WebsiteChannelName, nameof(ShippingRepository), nameof(GetShippingMethodsAsync));

        return await cache.LoadAsync(async cs =>
        {
            var result = await GetShippingInternalAsync(cancellationToken);
            var resultList = result.ToList();

            if (resultList.Count > 0)
            {
                cs.CacheDependency = cacheDependencyBuilderFactory.Create()
                    .ForInfoObjects<ShippingMethodInfo>()
                    .All()
                    .Builder()
                    .Build();
            }

            return resultList;
        }, cacheSettings);
    }

    /// <inheritdoc />
    public async Task<ShippingMethodInfo?> GetShippingByIdAsync(int shippingMethodId, CancellationToken cancellationToken = default)
    {
        if (shippingMethodId <= 0)
        {
            return null;
        }

        var methods = await GetShippingMethodsAsync(cancellationToken);
        return methods.FirstOrDefault(m => m.ShippingMethodID == shippingMethodId);
    }

    private async Task<IEnumerable<ShippingMethodInfo>> GetShippingInternalAsync(CancellationToken cancellationToken) =>
        await shippingMethodInfoProvider.Get()
            .WhereTrue(nameof(ShippingMethodInfo.ShippingMethodEnabled))
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken) ?? [];
}
