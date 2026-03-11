using Baseline.Ecommerce.Interfaces;
using Baseline.Ecommerce.Models;

using CMS.DataEngine;
using CMS.Helpers;
using CMS.Websites.Routing;

using CSharpFunctionalExtensions;

using ICacheDependencyBuilderFactory = CMS.Helpers.ICacheDependencyBuilderFactory;

namespace Baseline.Ecommerce.Services;

/// <summary>
/// Service for retrieving and working with fulfillment types.
/// Replaces the hardcoded ProductType enum with database-driven configuration.
/// </summary>
public class FulfillmentTypeService : IFulfillmentTypeService
{
    private readonly IInfoProvider<FulfillmentTypeInfo> _provider;
    private readonly IEnumerable<IFulfillmentTypeResolver> _resolvers;
    private readonly IProgressiveCache _cache;
    private readonly IWebsiteChannelContext _channelContext;
    private readonly ICacheDependencyBuilderFactory _cacheDependencyBuilderFactory;

    private const int CacheMinutes = 10;
    private const string DefaultFulfillmentType = "Physical";

    public FulfillmentTypeService(
        IInfoProvider<FulfillmentTypeInfo> provider,
        IEnumerable<IFulfillmentTypeResolver> resolvers,
        IProgressiveCache cache,
        IWebsiteChannelContext channelContext,
        ICacheDependencyBuilderFactory cacheDependencyBuilderFactory)
    {
        _provider = provider;
        _resolvers = resolvers;
        _cache = cache;
        _channelContext = channelContext;
        _cacheDependencyBuilderFactory = cacheDependencyBuilderFactory;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<FulfillmentTypeInfo>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        if (_channelContext.IsPreview)
        {
            return await GetAllInternalAsync(cancellationToken);
        }

        var cacheSettings = new CacheSettings(CacheMinutes, nameof(FulfillmentTypeService), nameof(GetAllAsync));

        return await _cache.LoadAsync(async cs =>
        {
            var result = (await GetAllInternalAsync(cancellationToken)).ToList();

            if (result.Count > 0)
            {
                cs.CacheDependency = _cacheDependencyBuilderFactory.Create()
                    .ForInfoObjects<FulfillmentTypeInfo>()
                    .All()
                    .Builder()
                    .Build();
            }

            return result;
        }, cacheSettings);
    }

    /// <inheritdoc />
    public async Task<Maybe<FulfillmentTypeInfo>> GetByCodeNameAsync(string codeName, CancellationToken cancellationToken = default)
    {
        var all = await GetAllAsync(cancellationToken);
        var match = all.FirstOrDefault(f => f.FulfillmentTypeCodeName.Equals(codeName, StringComparison.OrdinalIgnoreCase));
        return match != null ? Maybe.From(match) : Maybe<FulfillmentTypeInfo>.None;
    }

    /// <inheritdoc />
    public async Task<FulfillmentTypeInfo> GetForContentTypeAsync(string contentTypeName, CancellationToken cancellationToken = default)
    {
        // Check registered resolvers first
        foreach (var resolver in _resolvers)
        {
            var codeName = resolver.ResolveFulfillmentTypeCodeName(contentTypeName);
            if (!string.IsNullOrEmpty(codeName))
            {
                var resolved = await GetByCodeNameAsync(codeName, cancellationToken);
                if (resolved.HasValue)
                {
                    return resolved.Value;
                }
            }
        }

        // Fall back to convention-based matching
        var all = await GetAllAsync(cancellationToken);
        var allList = all.ToList();

        // Try to match by content type name containing the code name
        var conventionMatch = allList.FirstOrDefault(f =>
            contentTypeName.Contains(f.FulfillmentTypeCodeName, StringComparison.OrdinalIgnoreCase));

        if (conventionMatch != null)
        {
            return conventionMatch;
        }

        // Additional convention mappings
        var mappedCodeName = contentTypeName switch
        {
            _ when contentTypeName.Contains("Menu", StringComparison.OrdinalIgnoreCase) => "Food",
            _ when contentTypeName.Contains("Event", StringComparison.OrdinalIgnoreCase) => "Ticket",
            _ when contentTypeName.Contains("Gift", StringComparison.OrdinalIgnoreCase) => "GiftCard",
            _ => DefaultFulfillmentType
        };

        var fallback = await GetByCodeNameAsync(mappedCodeName, cancellationToken);
        if (fallback.HasValue)
        {
            return fallback.Value;
        }

        // Ultimate fallback - return Physical or first available
        return allList.FirstOrDefault(f => f.FulfillmentTypeCodeName == DefaultFulfillmentType)
            ?? allList.FirstOrDefault()
            ?? CreateDefaultFulfillmentType();
    }

    /// <inheritdoc />
    public async Task<bool> CartRequiresShippingAsync(IEnumerable<string> contentTypeNames, CancellationToken cancellationToken = default)
    {
        foreach (var typeName in contentTypeNames)
        {
            var fulfillmentType = await GetForContentTypeAsync(typeName, cancellationToken);
            if (fulfillmentType.FulfillmentTypeRequiresShipping)
            {
                return true;
            }
        }
        return false;
    }

    /// <inheritdoc />
    public async Task<string> GetCartFulfillmentDisplayNameAsync(IEnumerable<string> contentTypeNames, CancellationToken cancellationToken = default)
    {
        var typeNamesList = contentTypeNames.ToList();
        if (typeNamesList.Count == 0)
        {
            return "Standard";
        }

        var fulfillmentTypes = new List<FulfillmentTypeInfo>();
        foreach (var typeName in typeNamesList)
        {
            var ft = await GetForContentTypeAsync(typeName, cancellationToken);
            if (!fulfillmentTypes.Any(f => f.FulfillmentTypeID == ft.FulfillmentTypeID))
            {
                fulfillmentTypes.Add(ft);
            }
        }

        if (fulfillmentTypes.Count == 1)
        {
            return fulfillmentTypes.First().FulfillmentTypeDisplayName;
        }

        // Mixed cart - prioritize physical shipping if any physical items
        if (fulfillmentTypes.Any(ft => ft.FulfillmentTypeRequiresShipping))
        {
            return "Mixed (Shipping Required)";
        }

        return "Mixed (Digital/Pickup)";
    }

    private async Task<IEnumerable<FulfillmentTypeInfo>> GetAllInternalAsync(CancellationToken cancellationToken)
    {
        return await _provider.Get()
            .WhereTrue(nameof(FulfillmentTypeInfo.FulfillmentTypeIsEnabled))
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken) ?? [];
    }

    private static FulfillmentTypeInfo CreateDefaultFulfillmentType()
    {
        return new FulfillmentTypeInfo
        {
            FulfillmentTypeID = -1,
            FulfillmentTypeCodeName = DefaultFulfillmentType,
            FulfillmentTypeDisplayName = "Physical Product",
            FulfillmentTypeRequiresShipping = true,
            FulfillmentTypeRequiresBillingAddress = true,
            FulfillmentTypeSupportsDeliveryOptions = true,
            FulfillmentTypeIsEnabled = true
        };
    }
}
