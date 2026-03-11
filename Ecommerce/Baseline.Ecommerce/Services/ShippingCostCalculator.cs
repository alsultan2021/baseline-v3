using Baseline.Ecommerce.Models;
using CMS.Commerce;
using CMS.DataEngine;
using CMS.Helpers;
using CMS.Websites.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ICacheDependencyBuilderFactory = CMS.Helpers.ICacheDependencyBuilderFactory;

namespace Baseline.Ecommerce;

/// <summary>
/// Default implementation of <see cref="IShippingCostCalculator"/>.
/// Retrieves shipping methods from Kentico's ShippingMethodInfo and maps them to shipping rates.
/// </summary>
public class ShippingCostCalculator(
    IInfoProvider<ShippingMethodInfo> shippingMethodInfoProvider,
    IWebsiteChannelContext websiteChannelContext,
    IProgressiveCache cache,
    ICacheDependencyBuilderFactory cacheDependencyBuilderFactory,
    IOptions<BaselineEcommerceOptions> options,
    ILogger<ShippingCostCalculator> logger) : IShippingCostCalculator
{
    private const int CacheMinutes = 5;

    private readonly BaselineEcommerceOptions _options = options.Value;

    /// <inheritdoc/>
    public async Task<ShippingCostResult> CalculateAsync(ShippingCostRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Calculating shipping cost to {Country}, {State}",
            request.DestinationAddress.CountryCode, request.DestinationAddress.StateProvince);

        try
        {
            // Get shipping zone for destination
            var zone = await GetShippingZoneAsync(request.DestinationAddress, cancellationToken);
            if (zone == null)
            {
                logger.LogWarning("No shipping zone found for address: {Country}", request.DestinationAddress.CountryCode);
                return ShippingCostResult.Failed("Shipping is not available to this destination.");
            }

            // Calculate base cost
            var baseCost = zone.BaseRate;
            var breakdown = new List<ShippingCostComponent>
            {
                new("Base Rate", new Money { Amount = baseCost, Currency = _options.Pricing.DefaultCurrency })
            };

            // Add weight-based cost
            decimal totalWeight = request.Items.Sum(i => i.Weight * i.Quantity);
            if (totalWeight > 0 && zone.RatePerWeightUnit > 0)
            {
                var weightCost = totalWeight * zone.RatePerWeightUnit;
                baseCost += weightCost;
                breakdown.Add(new("Weight Charge", new Money { Amount = weightCost, Currency = _options.Pricing.DefaultCurrency },
                    $"{totalWeight:F2} units @ {zone.RatePerWeightUnit:C} per unit"));
            }

            // Add per-item cost
            int totalItems = request.Items.Sum(i => i.Quantity);
            if (totalItems > 0 && zone.RatePerItem > 0)
            {
                var itemCost = totalItems * zone.RatePerItem;
                baseCost += itemCost;
                breakdown.Add(new("Per-Item Charge", new Money { Amount = itemCost, Currency = _options.Pricing.DefaultCurrency },
                    $"{totalItems} items @ {zone.RatePerItem:C} per item"));
            }

            // Check for free shipping threshold
            if (zone.FreeShippingThreshold.HasValue &&
                request.Subtotal != null &&
                request.Subtotal.Amount >= zone.FreeShippingThreshold.Value)
            {
                logger.LogDebug("Order qualifies for free shipping (subtotal: {Subtotal}, threshold: {Threshold})",
                    request.Subtotal.Amount, zone.FreeShippingThreshold.Value);

                return new ShippingCostResult
                {
                    Success = true,
                    Cost = new Money { Amount = 0, Currency = _options.Pricing.DefaultCurrency },
                    Zone = zone,
                    CostBreakdown =
                    [
                        new("Free Shipping", new Money { Amount = 0, Currency = _options.Pricing.DefaultCurrency },
                            $"Order over {zone.FreeShippingThreshold.Value:C} qualifies for free shipping")
                    ]
                };
            }

            // Check for items with free shipping flag
            bool allFreeShipping = request.Items.All(i => i.FreeShipping);
            if (allFreeShipping && request.Items.Count > 0)
            {
                logger.LogDebug("All items qualify for free shipping");
                return ShippingCostResult.Successful(
                    new Money { Amount = 0, Currency = _options.Pricing.DefaultCurrency },
                    zone);
            }

            // Add special handling fees
            var specialHandlingItems = request.Items.Where(i => i.RequiresSpecialHandling || i.IsFragile).ToList();
            if (specialHandlingItems.Count > 0)
            {
                decimal specialHandlingFee = specialHandlingItems.Sum(i => i.Quantity) * 5.00m; // $5 per special item
                baseCost += specialHandlingFee;
                breakdown.Add(new("Special Handling", new Money { Amount = specialHandlingFee, Currency = _options.Pricing.DefaultCurrency }));
            }

            var result = new ShippingCostResult
            {
                Success = true,
                Cost = new Money { Amount = baseCost, Currency = _options.Pricing.DefaultCurrency },
                Zone = zone,
                CostBreakdown = breakdown,
                DeliveryEstimate = GetDeliveryEstimateFromZone(zone)
            };

            logger.LogDebug("Calculated shipping cost: {Cost}", baseCost);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error calculating shipping cost");
            return ShippingCostResult.Failed("An error occurred calculating shipping cost.");
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ShippingRate>> GetAvailableRatesAsync(ShippingRateRequest request, CancellationToken cancellationToken = default)
    {
        var shippingMethods = await GetShippingMethodsAsync(cancellationToken);
        var rates = new List<ShippingRate>();
        decimal? cheapestCost = null;

        foreach (var method in shippingMethods.Where(m => m.ShippingMethodEnabled))
        {
            // Calculate cost based on method's base price
            var cost = method.ShippingMethodPrice + (request.TotalWeight * _options.Shipping.RatePerWeightUnit);

            // Check for free shipping threshold
            if (_options.Shipping.FreeShippingThreshold.HasValue &&
                request.Subtotal != null &&
                request.Subtotal.Amount >= _options.Shipping.FreeShippingThreshold.Value)
            {
                cost = 0;
            }

            var rate = new ShippingRate
            {
                Method = new ShippingMethod
                {
                    Id = Guid.Parse(method.ShippingMethodGUID.ToString()),
                    Code = method.ShippingMethodName,
                    Name = method.ShippingMethodName,
                    Description = method.ShippingMethodDescription
                },
                Cost = new Money { Amount = cost, Currency = _options.Pricing.DefaultCurrency },
                DeliveryEstimate = GetDeliveryEstimateFromMethod(method),
                IsCheapest = false,
                IsFastest = false
            };

            rates.Add(rate);

            if (!cheapestCost.HasValue || cost < cheapestCost.Value)
            {
                cheapestCost = cost;
            }
        }

        // Mark cheapest and fastest
        if (rates.Count > 0)
        {
            var cheapest = rates.OrderBy(r => r.Cost.Amount).FirstOrDefault();
            var fastest = rates.OrderBy(r => r.DeliveryEstimate?.BusinessDays ?? int.MaxValue).FirstOrDefault();

            if (cheapest != null)
            {
                rates = rates.Select(r => r == cheapest ? r with { IsCheapest = true } : r).ToList();
            }
            if (fastest != null && fastest != cheapest)
            {
                rates = rates.Select(r => r == fastest ? r with { IsFastest = true } : r).ToList();
            }
        }

        return rates;
    }

    /// <inheritdoc/>
    public async Task<bool> IsShippingAvailableAsync(Address address, CancellationToken cancellationToken = default)
    {
        var zone = await GetShippingZoneAsync(address, cancellationToken);
        return zone != null && zone.IsEnabled;
    }

    /// <inheritdoc/>
    public async Task<ShippingZone?> GetShippingZoneAsync(Address address, CancellationToken cancellationToken = default)
    {
        var shippingMethods = await GetShippingMethodsAsync(cancellationToken);
        var enabledMethods = shippingMethods.Where(m => m.ShippingMethodEnabled).ToList();

        if (enabledMethods.Count == 0)
        {
            return null;
        }

        // Return the first enabled shipping method as a zone
        // In production, extend ShippingMethodInfo with custom fields for country/region filtering
        var method = enabledMethods.FirstOrDefault();
        return method != null ? MapToShippingZone(method) : null;
    }

    #region Private Helpers

    /// <summary>
    /// Gets cached shipping methods from Kentico.
    /// </summary>
    private async Task<IEnumerable<ShippingMethodInfo>> GetShippingMethodsAsync(CancellationToken cancellationToken)
    {
        if (websiteChannelContext.IsPreview)
        {
            return await GetShippingMethodsInternalAsync(cancellationToken);
        }

        var cacheSettings = new CacheSettings(
            CacheMinutes,
            websiteChannelContext.WebsiteChannelName,
            nameof(ShippingCostCalculator),
            nameof(GetShippingMethodsAsync));

        return await cache.LoadAsync(async cs =>
        {
            var result = await GetShippingMethodsInternalAsync(cancellationToken);
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

    private async Task<IEnumerable<ShippingMethodInfo>> GetShippingMethodsInternalAsync(CancellationToken cancellationToken) =>
        await shippingMethodInfoProvider.Get()
            .WhereTrue(nameof(ShippingMethodInfo.ShippingMethodEnabled))
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken) ?? [];

    /// <summary>
    /// Maps ShippingMethodInfo to ShippingZone model.
    /// </summary>
    private ShippingZone MapToShippingZone(ShippingMethodInfo method)
    {
        return new ShippingZone
        {
            Id = method.ShippingMethodGUID,
            Code = method.ShippingMethodName,
            Name = method.ShippingMethodName,
            Countries = [], // Can be extended via custom fields
            States = [],
            BaseRate = method.ShippingMethodPrice,
            RatePerWeightUnit = _options.Shipping.RatePerWeightUnit,
            RatePerItem = _options.Shipping.RatePerItem,
            FreeShippingThreshold = _options.Shipping.FreeShippingThreshold,
            IsEnabled = method.ShippingMethodEnabled,
            Order = method.ShippingMethodID
        };
    }

    private DeliveryEstimate GetDeliveryEstimateFromMethod(ShippingMethodInfo method)
    {
        // Use method name to determine delivery estimate
        // Can be extended with custom fields on ShippingMethodInfo
        var name = method.ShippingMethodName.ToUpperInvariant();

        return name switch
        {
            var n when n.Contains("EXPRESS") || n.Contains("FAST") =>
                new DeliveryEstimate(DateTime.Now.AddBusinessDays(2), DateTime.Now.AddBusinessDays(3), 3),
            var n when n.Contains("OVERNIGHT") || n.Contains("NEXT DAY") =>
                new DeliveryEstimate(DateTime.Now.AddBusinessDays(1), DateTime.Now.AddBusinessDays(1), 1),
            var n when n.Contains("ECONOMY") || n.Contains("SLOW") =>
                new DeliveryEstimate(DateTime.Now.AddBusinessDays(7), DateTime.Now.AddBusinessDays(14), 14),
            _ => new DeliveryEstimate(DateTime.Now.AddBusinessDays(5), DateTime.Now.AddBusinessDays(7), 7)
        };
    }

    private static DeliveryEstimate GetDeliveryEstimateFromZone(ShippingZone zone)
    {
        // Use zone code/name to determine delivery estimate
        var code = zone.Code?.ToUpperInvariant() ?? "";

        return code switch
        {
            var n when n.Contains("EXPRESS") || n.Contains("FAST") =>
                new DeliveryEstimate(DateTime.Now.AddBusinessDays(2), DateTime.Now.AddBusinessDays(3), 3),
            var n when n.Contains("OVERNIGHT") || n.Contains("NEXT DAY") =>
                new DeliveryEstimate(DateTime.Now.AddBusinessDays(1), DateTime.Now.AddBusinessDays(1), 1),
            var n when n.Contains("ECONOMY") || n.Contains("SLOW") =>
                new DeliveryEstimate(DateTime.Now.AddBusinessDays(7), DateTime.Now.AddBusinessDays(14), 14),
            var n when n.Contains("INTERNATIONAL") =>
                new DeliveryEstimate(DateTime.Now.AddBusinessDays(10), DateTime.Now.AddBusinessDays(21), 21),
            _ => new DeliveryEstimate(DateTime.Now.AddBusinessDays(5), DateTime.Now.AddBusinessDays(7), 7)
        };
    }

    #endregion
}

/// <summary>
/// Extension methods for date calculations.
/// </summary>
internal static class DateTimeBusinessDayExtensions
{
    /// <summary>
    /// Adds business days to a date.
    /// </summary>
    public static DateTime AddBusinessDays(this DateTime date, int days)
    {
        int direction = days < 0 ? -1 : 1;
        int remaining = Math.Abs(days);

        while (remaining > 0)
        {
            date = date.AddDays(direction);
            if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
            {
                remaining--;
            }
        }

        return date;
    }
}
