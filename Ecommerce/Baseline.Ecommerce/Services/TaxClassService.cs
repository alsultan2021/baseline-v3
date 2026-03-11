using Baseline.Ecommerce.Models;
using CMS.DataEngine;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baseline.Ecommerce;

/// <summary>
/// Default implementation of ITaxClassService.
/// Provides tax class retrieval and tax calculation operations.
/// </summary>
public class TaxClassService(
    IInfoProvider<TaxClassInfo> taxClassProvider,
    ICurrencyService currencyService,
    IMemoryCache cache,
    IOptions<BaselineEcommerceOptions> options,
    ILogger<TaxClassService> logger) : ITaxClassService
{
    private const string CacheTaxClasses = "Baseline.Ecommerce.TaxClasses";
    private const string CacheDefaultTaxClass = "Baseline.Ecommerce.DefaultTaxClass";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    private readonly BaselineEcommerceOptions _options = options.Value;

    /// <inheritdoc/>
    public async Task<IEnumerable<TaxClass>> GetTaxClassesAsync(CancellationToken cancellationToken = default)
    {
        var all = await GetAllTaxClassesAsync(cancellationToken);
        return all.Where(tc => tc.IsEnabled);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<TaxClass>> GetAllTaxClassesAsync(CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheTaxClasses;

        if (cache.TryGetValue<IEnumerable<TaxClass>>(cacheKey, out var cached) && cached != null)
        {
            return cached;
        }

        logger.LogDebug("Loading tax classes from database");

        try
        {
            var taxClassInfos = await taxClassProvider.Get()
                .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

            // Order in memory: default first, then by order, then by name
            var taxClasses = taxClassInfos
                .OrderByDescending(tc => tc.TaxClassIsDefault)
                .ThenBy(tc => tc.TaxClassOrder)
                .ThenBy(tc => tc.TaxClassDisplayName)
                .Select(MapToTaxClass)
                .ToList();

            cache.Set(cacheKey, taxClasses, CacheDuration);

            return taxClasses;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load tax classes");
            return [];
        }
    }

    /// <inheritdoc/>
    public async Task<TaxClass?> GetDefaultTaxClassAsync(CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheDefaultTaxClass;

        if (cache.TryGetValue<TaxClass>(cacheKey, out var cached) && cached != null)
        {
            return cached;
        }

        logger.LogDebug("Loading default tax class from database");

        try
        {
            var taxClassInfo = await taxClassProvider.Get()
                .WhereEquals(nameof(TaxClassInfo.TaxClassIsDefault), true)
                .WhereEquals(nameof(TaxClassInfo.TaxClassEnabled), true)
                .TopN(1)
                .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

            var defaultTaxClass = taxClassInfo.FirstOrDefault();

            if (defaultTaxClass == null)
            {
                // Fall back to first enabled tax class
                defaultTaxClass = (await taxClassProvider.Get()
                    .WhereEquals(nameof(TaxClassInfo.TaxClassEnabled), true)
                    .OrderBy(nameof(TaxClassInfo.TaxClassOrder))
                    .TopN(1)
                    .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken))
                    .FirstOrDefault();
            }

            if (defaultTaxClass == null)
            {
                logger.LogWarning("No default tax class found, returning fallback from options");
                return CreateFallbackTaxClass();
            }

            var taxClass = MapToTaxClass(defaultTaxClass);
            cache.Set(cacheKey, taxClass, CacheDuration);

            return taxClass;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load default tax class");
            return CreateFallbackTaxClass();
        }
    }

    /// <inheritdoc/>
    public async Task<TaxClass?> GetTaxClassByCodeAsync(string taxClassCode, CancellationToken cancellationToken = default)
    {
        var taxClasses = await GetAllTaxClassesAsync(cancellationToken);
        return taxClasses.FirstOrDefault(tc => tc.Code.Equals(taxClassCode, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc/>
    public async Task<TaxClass?> GetTaxClassByIdAsync(int taxClassId, CancellationToken cancellationToken = default)
    {
        var taxClasses = await GetAllTaxClassesAsync(cancellationToken);
        return taxClasses.FirstOrDefault(tc => tc.Id == taxClassId);
    }

    /// <inheritdoc/>
    public async Task<TaxClass?> GetTaxClassByGuidAsync(Guid taxClassGuid, CancellationToken cancellationToken = default)
    {
        if (taxClassGuid == Guid.Empty)
        {
            return null;
        }

        var taxClasses = await GetAllTaxClassesAsync(cancellationToken);
        return taxClasses.FirstOrDefault(tc => tc.Guid == taxClassGuid);
    }

    /// <inheritdoc/>
    public async Task<Money> CalculateTaxAsync(Money subtotal, int taxClassId, CancellationToken cancellationToken = default)
    {
        var taxClass = await GetTaxClassByIdAsync(taxClassId, cancellationToken);

        if (taxClass == null)
        {
            logger.LogWarning("Tax class {TaxClassId} not found, using default", taxClassId);
            return await CalculateTaxAsync(subtotal, cancellationToken);
        }

        var taxAmount = subtotal.Amount * taxClass.DefaultRate;

        // Round according to currency
        var currency = await currencyService.GetCurrencyByCodeAsync(subtotal.Currency, cancellationToken);
        if (currency != null)
        {
            taxAmount = currencyService.RoundAmount(taxAmount, currency);
        }
        else
        {
            taxAmount = Math.Round(taxAmount, 2, MidpointRounding.AwayFromZero);
        }

        return new Money
        {
            Amount = taxAmount,
            Currency = subtotal.Currency
        };
    }

    /// <inheritdoc/>
    public async Task<Money> CalculateTaxAsync(Money subtotal, CancellationToken cancellationToken = default)
    {
        var defaultTaxClass = await GetDefaultTaxClassAsync(cancellationToken);

        var taxRate = defaultTaxClass?.DefaultRate ?? _options.Pricing.DefaultTaxRate;
        var taxAmount = subtotal.Amount * taxRate;

        // Round according to currency
        var currency = await currencyService.GetCurrencyByCodeAsync(subtotal.Currency, cancellationToken);
        if (currency != null)
        {
            taxAmount = currencyService.RoundAmount(taxAmount, currency);
        }
        else
        {
            taxAmount = Math.Round(taxAmount, 2, MidpointRounding.AwayFromZero);
        }

        return new Money
        {
            Amount = taxAmount,
            Currency = subtotal.Currency
        };
    }

    /// <inheritdoc/>
    public async Task<decimal> GetEffectiveTaxRateAsync(
        int taxClassId,
        string? regionCode = null,
        CancellationToken cancellationToken = default)
    {
        var taxClass = await GetTaxClassByIdAsync(taxClassId, cancellationToken);

        if (taxClass == null)
        {
            logger.LogWarning("Tax class {TaxClassId} not found, using default rate from options", taxClassId);
            return _options.Pricing.DefaultTaxRate;
        }

        // If regional tax rates are configured in options, check for regional override
        if (!string.IsNullOrEmpty(regionCode) && _options.Pricing.TaxRatesByRegion?.Count > 0)
        {
            if (_options.Pricing.TaxRatesByRegion.TryGetValue(regionCode, out var regionalRate))
            {
                logger.LogDebug("Using regional tax rate {Rate} for region {Region}", regionalRate, regionCode);
                return regionalRate;
            }
        }

        return taxClass.DefaultRate;
    }

    #region Private Helpers

    private static TaxClass MapToTaxClass(TaxClassInfo info) => new()
    {
        Id = info.TaxClassID,
        Guid = info.TaxClassGuid,
        Code = info.TaxClassName,
        DisplayName = info.TaxClassDisplayName,
        Description = info.TaxClassDescription,
        DefaultRate = info.TaxClassDefaultRate / 100m,
        IsDefault = info.TaxClassIsDefault,
        IsEnabled = info.TaxClassEnabled,
        Order = info.TaxClassOrder
    };

    private TaxClass CreateFallbackTaxClass() => new()
    {
        Id = 0,
        Guid = Guid.Empty,
        Code = "STANDARD",
        DisplayName = "Standard Tax",
        Description = "Default tax class",
        DefaultRate = _options.Pricing.DefaultTaxRate,
        IsDefault = true,
        IsEnabled = true,
        Order = 0
    };

    #endregion
}
