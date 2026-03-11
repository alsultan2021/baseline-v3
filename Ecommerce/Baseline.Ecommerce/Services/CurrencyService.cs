using Baseline.Ecommerce.Models;
using CMS.DataEngine;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
using XperienceCommunity.ChannelSettings.Repositories;

namespace Baseline.Ecommerce;

/// <summary>
/// Default implementation of ICurrencyService.
/// Provides currency retrieval and formatting operations.
/// Uses channel settings for formatting configuration.
/// </summary>
public class CurrencyService(
    IInfoProvider<CurrencyInfo> currencyProvider,
    IChannelCustomSettingsRepository channelSettingsRepository,
    IMemoryCache cache,
    IOptions<BaselineEcommerceOptions> options,
    ILogger<CurrencyService> logger) : ICurrencyService
{
    private const string CacheCurrencies = "Baseline.Ecommerce.Currencies";
    private const string CacheDefaultCurrency = "Baseline.Ecommerce.DefaultCurrency";
    private const string CacheChannelSettings = "CurrencyService_CommerceChannelSettings";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan SettingsCacheDuration = TimeSpan.FromMinutes(5);

    private readonly BaselineEcommerceOptions _options = options.Value;

    /// <inheritdoc/>
    public async Task<IEnumerable<Currency>> GetCurrenciesAsync(CancellationToken cancellationToken = default)
    {
        var all = await GetAllCurrenciesAsync(cancellationToken);
        return all.Where(c => c.IsEnabled);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Currency>> GetAllCurrenciesAsync(CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheCurrencies;

        if (cache.TryGetValue<IEnumerable<Currency>>(cacheKey, out var cached) && cached != null)
        {
            return cached;
        }

        logger.LogDebug("Loading currencies from database");

        try
        {
            var currencyInfos = await currencyProvider.Get()
                .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

            // Order by display order, then by name
            var currencies = currencyInfos
                .OrderBy(c => c.CurrencyOrder)
                .ThenBy(c => c.CurrencyDisplayName)
                .Select(MapToCurrency)
                .ToList();

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(CacheDuration)
                .SetSize(1);
            cache.Set(cacheKey, currencies, cacheOptions);

            return currencies;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load currencies");
            return [];
        }
    }

    /// <inheritdoc/>
    public async Task<Currency?> GetDefaultCurrencyAsync(CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheDefaultCurrency;

        if (cache.TryGetValue<Currency>(cacheKey, out var cached) && cached != null)
        {
            return cached;
        }

        logger.LogDebug("Loading default currency from channel settings");

        try
        {
            // Get default currency code from CommerceChannelSettings
            var settings = await channelSettingsRepository.GetSettingsModel<CommerceChannelSettings>();
            var defaultCurrencyCode = settings?.DefaultCurrency;

            CurrencyInfo? defaultCurrencyInfo = null;

            // Look up by code if configured
            if (!string.IsNullOrWhiteSpace(defaultCurrencyCode))
            {
                var result = await currencyProvider.Get()
                    .WhereEquals(nameof(CurrencyInfo.CurrencyCode), defaultCurrencyCode)
                    .WhereEquals(nameof(CurrencyInfo.CurrencyEnabled), true)
                    .TopN(1)
                    .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

                defaultCurrencyInfo = result.FirstOrDefault();

                if (defaultCurrencyInfo == null)
                {
                    logger.LogWarning("Configured default currency '{CurrencyCode}' not found or not enabled", defaultCurrencyCode);
                }
            }

            // Fall back to first enabled currency by order
            if (defaultCurrencyInfo == null)
            {
                defaultCurrencyInfo = (await currencyProvider.Get()
                    .WhereEquals(nameof(CurrencyInfo.CurrencyEnabled), true)
                    .OrderBy(nameof(CurrencyInfo.CurrencyOrder))
                    .TopN(1)
                    .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken))
                    .FirstOrDefault();
            }

            if (defaultCurrencyInfo == null)
            {
                logger.LogWarning("No default currency found, returning fallback currency from options");
                return CreateFallbackCurrency();
            }

            var currency = MapToCurrency(defaultCurrencyInfo);
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(CacheDuration)
                .SetSize(1);
            cache.Set(cacheKey, currency, cacheOptions);

            return currency;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load default currency");
            return CreateFallbackCurrency();
        }
    }

    /// <inheritdoc/>
    public async Task<Currency?> GetCurrencyByCodeAsync(string currencyCode, CancellationToken cancellationToken = default)
    {
        var currencies = await GetAllCurrenciesAsync(cancellationToken);
        return currencies.FirstOrDefault(c => c.Code.Equals(currencyCode, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc/>
    public async Task<Currency?> GetCurrencyByIdAsync(int currencyId, CancellationToken cancellationToken = default)
    {
        var currencies = await GetAllCurrenciesAsync(cancellationToken);
        return currencies.FirstOrDefault(c => c.Id == currencyId);
    }

    /// <inheritdoc/>
    public string FormatAmount(decimal amount, Currency currency)
    {
        // For sync method, we can't read channel settings asynchronously
        // Use default formatting from currency model
        return FormatAmountCore(amount, currency, null);
    }

    /// <inheritdoc/>
    public async Task<string> FormatAmountAsync(decimal amount, CancellationToken cancellationToken = default)
    {
        var currency = await GetDefaultCurrencyAsync(cancellationToken);
        currency ??= CreateFallbackCurrency();

        // Get channel settings for formatting preferences
        var settings = await GetChannelSettingsAsync();
        return FormatAmountCore(amount, currency, settings);
    }

    /// <summary>
    /// Formats an amount using the specified currency with optional channel settings override.
    /// </summary>
    public async Task<string> FormatAmountWithSettingsAsync(decimal amount, Currency currency, CancellationToken cancellationToken = default)
    {
        var settings = await GetChannelSettingsAsync();
        return FormatAmountCore(amount, currency, settings);
    }

    /// <summary>
    /// Core formatting logic that applies channel settings if provided.
    /// </summary>
    private string FormatAmountCore(decimal amount, Currency currency, CommerceChannelSettings? settings)
    {
        // Determine decimal places: channel settings override, else currency, else default 2
        int decimalPlaces = settings?.DecimalPlaces ?? currency.DecimalPlaces;
        if (decimalPlaces < 0) decimalPlaces = 2;

        var roundedAmount = Math.Round(amount, decimalPlaces, MidpointRounding.AwayFromZero);

        // Determine separators from channel settings or use defaults
        string thousandsSep = settings?.ThousandsSeparator ?? ",";
        string decimalSep = settings?.DecimalSeparator ?? ".";
        string symbolPosition = settings?.CurrencySymbolPosition ?? "Before";

        // Build custom number format
        var numberFormat = new NumberFormatInfo
        {
            NumberDecimalDigits = decimalPlaces,
            NumberDecimalSeparator = string.IsNullOrEmpty(decimalSep) ? "." : decimalSep,
            NumberGroupSeparator = string.IsNullOrEmpty(thousandsSep) ? "," : thousandsSep,
            NumberGroupSizes = [3]
        };

        var formattedNumber = roundedAmount.ToString("N", numberFormat);

        // Apply symbol position
        return symbolPosition.Equals("After", StringComparison.OrdinalIgnoreCase)
            ? $"{formattedNumber}{currency.Symbol}"
            : $"{currency.Symbol}{formattedNumber}";
    }

    /// <summary>
    /// Gets commerce channel settings with caching.
    /// </summary>
    private async Task<CommerceChannelSettings> GetChannelSettingsAsync()
    {
        if (cache.TryGetValue(CacheChannelSettings, out CommerceChannelSettings? cachedSettings) && cachedSettings != null)
        {
            return cachedSettings;
        }

        var settings = await channelSettingsRepository.GetSettingsModel<CommerceChannelSettings>();
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(SettingsCacheDuration)
            .SetSize(1);
        cache.Set(CacheChannelSettings, settings, cacheOptions);
        return settings;
    }

    /// <inheritdoc/>
    public decimal RoundAmount(decimal amount, Currency currency)
    {
        return Math.Round(amount, currency.DecimalPlaces, MidpointRounding.AwayFromZero);
    }

    #region Private Helpers

    private static Currency MapToCurrency(CurrencyInfo info) => new()
    {
        Id = info.CurrencyID,
        Guid = info.CurrencyGuid,
        Code = info.CurrencyCode,
        DisplayName = info.CurrencyDisplayName,
        Symbol = info.CurrencySymbol,
        DecimalPlaces = info.CurrencyDecimalPlaces,
        FormatPattern = info.CurrencyFormatPattern,
        IsDefault = info.CurrencyIsDefault,
        IsEnabled = info.CurrencyEnabled,
        Order = info.CurrencyOrder
    };

    private Currency CreateFallbackCurrency() => new()
    {
        Id = 0,
        Guid = Guid.Empty,
        Code = _options.Pricing.DefaultCurrency,
        DisplayName = _options.Pricing.DefaultCurrency,
        Symbol = GetSymbolForCode(_options.Pricing.DefaultCurrency),
        DecimalPlaces = 2,
        FormatPattern = "${0:N2}",
        IsDefault = true,
        IsEnabled = true,
        Order = 0
    };

    private static string GetSymbolForCode(string currencyCode) => currencyCode.ToUpperInvariant() switch
    {
        "USD" => "$",
        "EUR" => "€",
        "GBP" => "£",
        "CAD" => "$",
        "AUD" => "$",
        "JPY" => "¥",
        "CHF" => "CHF",
        _ => "$"
    };

    #endregion
}
