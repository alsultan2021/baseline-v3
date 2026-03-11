using CMS.Globalization;
using CMS.DataEngine;
using CMS.Helpers;
using CMS.Websites.Routing;
using Ecommerce.Models;
using Ecommerce.Services;

using ICacheDependencyBuilderFactory = CMS.Helpers.ICacheDependencyBuilderFactory;

namespace Baseline.Ecommerce;

/// <summary>
/// v3 implementation of ICountryStateRepository using Xperience by Kentico globalization APIs.
/// Provides cached country and state data for address forms in checkout.
/// </summary>
public sealed class CountryStateRepository(
    IInfoProvider<CountryInfo> countryInfoProvider,
    IInfoProvider<StateInfo> stateInfoProvider,
    IWebsiteChannelContext websiteChannelContext,
    IProgressiveCache cache,
    ICacheDependencyBuilderFactory cacheDependencyBuilderFactory) : ICountryStateRepository
{
    private const int CacheMinutes = 60;

    /// <inheritdoc/>
    public async Task<IEnumerable<CountryState>> GetCountriesAsync(CancellationToken cancellationToken = default)
    {
        if (websiteChannelContext.IsPreview)
        {
            return await GetCountriesInternal(cancellationToken);
        }

        var cacheSettings = new CacheSettings(CacheMinutes, websiteChannelContext.WebsiteChannelName, nameof(CountryStateRepository), nameof(GetCountriesAsync));

        return await cache.LoadAsync(async cs =>
        {
            var result = await GetCountriesInternal(cancellationToken);
            var resultList = result.ToList();

            if (resultList.Count > 0)
            {
                cs.CacheDependency = cacheDependencyBuilderFactory.Create()
                    .ForInfoObjects<CountryInfo>()
                    .All()
                    .Builder()
                    .Build();
            }

            return resultList;
        }, cacheSettings);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<CountryState>> GetStatesAsync(int countryId, CancellationToken cancellationToken = default)
    {
        if (websiteChannelContext.IsPreview)
        {
            return await GetStatesInternal(countryId, cancellationToken);
        }

        var cacheSettings = new CacheSettings(CacheMinutes, websiteChannelContext.WebsiteChannelName, nameof(CountryStateRepository), nameof(GetStatesAsync), countryId);

        return await cache.LoadAsync(async cs =>
        {
            var result = await GetStatesInternal(countryId, cancellationToken);
            var resultList = result.ToList();

            if (resultList.Count > 0)
            {
                cs.CacheDependency = cacheDependencyBuilderFactory.Create()
                    .ForInfoObjects<StateInfo>()
                    .All()
                    .Builder()
                    .Build();
            }

            return resultList;
        }, cacheSettings);
    }

    private async Task<IEnumerable<CountryState>> GetCountriesInternal(CancellationToken cancellationToken)
    {
        try
        {
            var countries = await countryInfoProvider
                .Get()
                .OrderBy(nameof(CountryInfo.CountryDisplayName))
                .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

            return countries.Select(c => new CountryState
            {
                CountryId = c.CountryID,
                CountryCode = c.CountryTwoLetterCode ?? c.CountryThreeLetterCode ?? string.Empty,
                CountryName = c.CountryDisplayName,
                DisplayName = c.CountryDisplayName
            });
        }
        catch (OperationCanceledException)
        {
            return [];
        }
    }

    private async Task<IEnumerable<CountryState>> GetStatesInternal(int countryId, CancellationToken cancellationToken)
    {
        try
        {
            var states = await stateInfoProvider
                .Get()
                .WhereEquals(nameof(StateInfo.CountryID), countryId)
                .OrderBy(nameof(StateInfo.StateDisplayName))
                .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

            return states.Select(s => new CountryState
            {
                StateId = s.StateID,
                StateCode = s.StateCode,
                StateName = s.StateDisplayName,
                DisplayName = s.StateDisplayName,
                CountryId = countryId
            });
        }
        catch (OperationCanceledException)
        {
            return [];
        }
    }
}
