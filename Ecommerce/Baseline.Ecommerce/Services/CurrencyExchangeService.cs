using Baseline.Ecommerce.Models;
using CMS.DataEngine;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Baseline.Ecommerce;

/// <summary>
/// Default implementation of ICurrencyExchangeService.
/// Provides currency conversion operations using stored exchange rates.
/// </summary>
public class CurrencyExchangeService(
    IInfoProvider<CurrencyExchangeRateInfo> exchangeRateProvider,
    ICurrencyService currencyService,
    IMemoryCache cache,
    ILogger<CurrencyExchangeService> logger) : ICurrencyExchangeService
{
    private const string CacheExchangeRates = "Baseline.Ecommerce.ExchangeRates";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(15);

    /// <inheritdoc/>
    public async Task<CurrencyExchangeRate?> GetExchangeRateAsync(
        string fromCurrencyCode,
        string toCurrencyCode,
        CancellationToken cancellationToken = default)
    {
        var rates = await GetAllExchangeRatesAsync(cancellationToken);

        return rates.FirstOrDefault(r =>
            r.FromCurrencyCode.Equals(fromCurrencyCode, StringComparison.OrdinalIgnoreCase) &&
            r.ToCurrencyCode.Equals(toCurrencyCode, StringComparison.OrdinalIgnoreCase) &&
            r.IsActive);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<CurrencyExchangeRate>> GetExchangeRatesFromAsync(
        string fromCurrencyCode,
        CancellationToken cancellationToken = default)
    {
        var rates = await GetAllExchangeRatesAsync(cancellationToken);

        return rates.Where(r =>
            r.FromCurrencyCode.Equals(fromCurrencyCode, StringComparison.OrdinalIgnoreCase) &&
            r.IsActive);
    }

    /// <inheritdoc/>
    public async Task<Money> ConvertAsync(
        Money amount,
        string toCurrencyCode,
        CancellationToken cancellationToken = default)
    {
        if (amount.Currency.Equals(toCurrencyCode, StringComparison.OrdinalIgnoreCase))
        {
            return amount;
        }

        var exchangeRate = await GetExchangeRateAsync(amount.Currency, toCurrencyCode, cancellationToken);

        if (exchangeRate == null)
        {
            logger.LogDebug(
                "No exchange rate found from {FromCurrency} to {ToCurrency}, using 1:1 fallback",
                amount.Currency,
                toCurrencyCode);

            // Return the amount as-is but with the target currency (1:1 fallback)
            return new Money { Amount = amount.Amount, Currency = toCurrencyCode };
        }

        return Convert(amount, exchangeRate);
    }

    /// <inheritdoc/>
    public Money Convert(Money amount, CurrencyExchangeRate exchangeRate)
    {
        var convertedAmount = amount.Amount * exchangeRate.Rate;

        // Round to target currency decimal places if we have the currency info
        if (exchangeRate.ToCurrency != null)
        {
            convertedAmount = Math.Round(convertedAmount, exchangeRate.ToCurrency.DecimalPlaces, MidpointRounding.AwayFromZero);
        }

        return new Money
        {
            Amount = convertedAmount,
            Currency = exchangeRate.ToCurrencyCode
        };
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<CurrencyExchangeRate>> GetAllExchangeRatesAsync(CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheExchangeRates;

        if (cache.TryGetValue<IEnumerable<CurrencyExchangeRate>>(cacheKey, out var cached) && cached != null)
        {
            return cached;
        }

        logger.LogDebug("Loading exchange rates from database");

        try
        {
            var exchangeRateInfos = await exchangeRateProvider.Get()
                .WhereEquals(nameof(CurrencyExchangeRateInfo.ExchangeRateEnabled), true)
                .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

            // Load currencies for mapping
            var currencies = (await currencyService.GetAllCurrenciesAsync(cancellationToken)).ToDictionary(c => c.Id);

            var exchangeRates = exchangeRateInfos.Select(info => MapToExchangeRate(info, currencies)).ToList();

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(CacheDuration)
                .SetSize(1);
            cache.Set(cacheKey, exchangeRates, cacheOptions);

            return exchangeRates;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load exchange rates");
            return [];
        }
    }

    /// <inheritdoc/>
    public async Task<Money> ConvertToDefaultAsync(Money amount, CancellationToken cancellationToken = default)
    {
        var defaultCurrency = await currencyService.GetDefaultCurrencyAsync(cancellationToken);

        if (defaultCurrency == null)
        {
            logger.LogWarning("No default currency configured, returning amount as-is");
            return amount;
        }

        return await ConvertAsync(amount, defaultCurrency.Code, cancellationToken);
    }

    #region Private Helpers

    private static CurrencyExchangeRate MapToExchangeRate(
        CurrencyExchangeRateInfo info,
        Dictionary<int, Currency> currencies)
    {
        currencies.TryGetValue(info.ExchangeRateFromCurrencyID, out var fromCurrency);
        currencies.TryGetValue(info.ExchangeRateToCurrencyID, out var toCurrency);

        return new CurrencyExchangeRate
        {
            Id = info.ExchangeRateID,
            Guid = info.ExchangeRateGuid,
            FromCurrencyCode = fromCurrency?.Code ?? string.Empty,
            ToCurrencyCode = toCurrency?.Code ?? string.Empty,
            FromCurrency = fromCurrency,
            ToCurrency = toCurrency,
            Rate = info.ExchangeRateValue,
            EffectiveFrom = info.ExchangeRateValidFrom,
            EffectiveTo = info.ExchangeRateValidTo,
            IsActive = info.ExchangeRateEnabled,
            Order = 0
        };
    }

    #endregion
}
