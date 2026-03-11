using Baseline.Ecommerce.Models;
using CMS.Commerce;
using CMS.DataEngine;
using CMS.Websites.Routing;
using Ecommerce.Services;
using Kentico.Content.Web.Mvc.Routing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using XperienceCommunity.ChannelSettings.Repositories;

namespace Baseline.Ecommerce;

/// <summary>
/// Default implementation of IPricingService.
/// Integrates with IProductRepository for product price lookups, ICurrencyService for formatting,
/// and IInfoProvider for shipping costs. Uses CommerceChannelSettings for tax configuration.
/// Evaluates BOTH Kentico DC and Baseline promotion engines, picking the better discount.
/// </summary>
public class PricingService(
    IProductRepository productRepository,
    IInfoProvider<ShippingMethodInfo> shippingMethodInfoProvider,
    IPromotionService promotionService,
    ICurrencyService currencyService,
    ITaxClassService taxClassService,
    IChannelCustomSettingsRepository channelSettingsRepository,
    IMemoryCache cache,
    IOptions<BaselineEcommerceOptions> options,
    IOptions<TaxCalculationOptions> taxOptions,
    KenticoDcPromotionHelper dcPromotionHelper,
    IPreferredLanguageRetriever languageRetriever,
    ILogger<PricingService> logger) : IPricingService
{
    private readonly BaselineEcommerceOptions _options = options.Value;
    private readonly TaxCalculationOptions _taxOptions = taxOptions.Value;

    // Cache key for channel settings
    private const string ChannelSettingsCacheKey = "baseline.ecommerce.channelsettings";

    // Cache keys and expiry for promotion lookups
    private const string CatalogPromotionsCacheKey = "baseline.ecommerce.promotions.catalog";
    private const string OrderPromotionsCacheKey = "baseline.ecommerce.promotions.order";
    private static readonly TimeSpan PromotionCacheExpiry = TimeSpan.FromMinutes(2);

    /// <inheritdoc/>
    public async Task<PriceCalculationResult> CalculateAsync(
        PriceCalculationRequest request,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Calculating prices: Mode={Mode}, ItemCount={ItemCount}",
            request.Mode, request.Items.Count);

        var resultItems = new List<PriceCalculationResultItem>();
        var catalogPromotions = new List<AppliedPromotion>();
        var orderPromotions = new List<AppliedPromotion>();
        decimal totalPrice = 0;
        decimal totalCatalogDiscount = 0;
        decimal totalOrderDiscount = 0;
        decimal totalTax = 0;
        decimal shippingPrice = 0;

        // Batch-load all products once to avoid N+1 queries in per-item calculations
        var allProductIds = request.Items
            .Where(i => !i.OverridePrice.HasValue && int.TryParse(i.ProductIdentifier, out _))
            .Select(i => int.Parse(i.ProductIdentifier))
            .Distinct()
            .ToList();

        var allProducts = allProductIds.Count > 0
            ? await productRepository.GetProductsByIdsAsync(allProductIds, cancellationToken)
            : Enumerable.Empty<object>();

        // Build lookup keyed by ContentItemID for O(1) access
        var productLookup = ProductFieldHelper.BuildLookup(allProducts);

        // Step 1: Calculate base prices using pre-loaded products (All modes)
        foreach (var item in request.Items)
        {
            var itemResult = CalculateItemPrice(item, productLookup);
            resultItems.Add(itemResult);
            totalPrice += itemResult.LineSubtotal;
        }

        // Step 2: Apply catalog promotions (All modes)
        var dcLineItems = request.Items.Select(i =>
            new KenticoDcPromotionHelper.LineItem(
                int.TryParse(i.ProductIdentifier, out var id) ? id : 0,
                i.Quantity))
            .ToList();
        var language = languageRetriever.Get();

        if (request.Mode >= PriceCalculationMode.Catalog)
        {
            // Evaluate BOTH promotion engines and pick the better total discount.
            // DC promotions (admin-configured) and Baseline promotions (PromotionInfo table)
            // are independent data stores — always compare both.

            // 1. Evaluate Kentico DC catalog promotions
            decimal dcCatalogDiscount = 0;
            var dcCatalog = await dcPromotionHelper.GetCatalogPromotionsAsync(
                dcLineItems, request.CouponCodes, request.CustomerId ?? 0, language, cancellationToken);
            if (dcCatalog.HasValue)
                dcCatalogDiscount = dcCatalog.Value.TotalDiscount;

            // 2. Evaluate Baseline custom catalog promotions (on a copy to avoid mutation)
            decimal baselineCatalogDiscount = 0;
            var baselineResultItems = resultItems.ToList();
            var baselineCatalogPromotions = new List<AppliedPromotion>();
            var activePromotions = await GetCachedCatalogPromotionsAsync(cancellationToken);
            foreach (var promo in activePromotions)
            {
                var (discount, applied) = ApplyCatalogPromotion(promo, baselineResultItems);
                if (applied != null)
                {
                    baselineCatalogPromotions.Add(applied);
                    baselineCatalogDiscount += discount;
                }
            }

            // 3. Pick whichever engine gives the better total catalog discount
            if (dcCatalogDiscount >= baselineCatalogDiscount && dcCatalogDiscount > 0)
            {
                totalCatalogDiscount = dcCatalogDiscount;
                foreach (var itemDiscount in dcCatalog!.Value.PerItemDiscounts)
                {
                    var idx = resultItems.FindIndex(r =>
                        int.TryParse(r.ProductIdentifier, out var pid) && pid == itemDiscount.ProductId);
                    if (idx >= 0)
                    {
                        var ri = resultItems[idx];
                        resultItems[idx] = ri with
                        {
                            Discount = itemDiscount.LineDiscount,
                            UnitPrice = ri.OriginalUnitPrice - itemDiscount.UnitDiscount,
                            LineSubtotal = ri.LineSubtotal - itemDiscount.LineDiscount,
                            CatalogPromotion = new AppliedPromotion
                            {
                                PromotionId = "kentico-dc-catalog",
                                Name = itemDiscount.Label,
                                DiscountAmount = itemDiscount.LineDiscount,
                                IsCatalogPromotion = true
                            }
                        };
                    }
                    catalogPromotions.Add(new AppliedPromotion
                    {
                        PromotionId = "kentico-dc-catalog",
                        Name = itemDiscount.Label,
                        DiscountAmount = itemDiscount.LineDiscount,
                        IsCatalogPromotion = true
                    });
                }
                logger.LogDebug("Kentico DC catalog discounts won: {Count} promotions, total {Amount:C}",
                    dcCatalog.Value.PerItemDiscounts.Count, totalCatalogDiscount);
            }
            else if (baselineCatalogDiscount > 0)
            {
                totalCatalogDiscount = baselineCatalogDiscount;
                resultItems = baselineResultItems;
                catalogPromotions.AddRange(baselineCatalogPromotions);
                logger.LogDebug("Baseline catalog discounts won: {Count} promotions, total {Amount:C}",
                    baselineCatalogPromotions.Count, totalCatalogDiscount);
            }

            totalPrice -= totalCatalogDiscount;
        }

        // Step 3: Apply order promotions (ShoppingCart and Checkout modes)
        var taxEntries = new List<TaxEntryInfo>();
        if (request.Mode >= PriceCalculationMode.ShoppingCart)
        {
            // Evaluate BOTH engines and pick the better order discount

            // 1. Kentico DC order promotions
            decimal dcOrderDiscount = 0;
            string? dcOrderName = null;
            var dcOrder = await dcPromotionHelper.GetOrderPromotionAsync(
                dcLineItems, request.CouponCodes, request.CustomerId ?? 0, language, cancellationToken);
            if (dcOrder.HasValue)
            {
                dcOrderDiscount = dcOrder.Value.Amount;
                dcOrderName = dcOrder.Value.Name;
            }

            // 2. Baseline custom order promotions
            decimal baselineOrderDiscount = 0;
            var baselineOrderPromotions = new List<AppliedPromotion>();
            var orderPromos = await GetCachedOrderPromotionsAsync(cancellationToken);
            foreach (var promo in orderPromos)
            {
                var (discount, applied) = ApplyOrderPromotion(promo, totalPrice, request.CouponCodes);
                if (applied != null)
                {
                    baselineOrderPromotions.Add(applied);
                    baselineOrderDiscount += discount;
                }
            }

            // 3. Pick whichever engine gives the better order discount
            if (dcOrderDiscount >= baselineOrderDiscount && dcOrderDiscount > 0)
            {
                orderPromotions.Add(new AppliedPromotion
                {
                    PromotionId = "kentico-dc-order",
                    Name = dcOrderName!,
                    DiscountAmount = dcOrderDiscount
                });
                totalOrderDiscount = dcOrderDiscount;
                logger.LogDebug("Kentico DC order discount won: {Name} = {Amount:C}", dcOrderName, dcOrderDiscount);
            }
            else if (baselineOrderDiscount > 0)
            {
                orderPromotions.AddRange(baselineOrderPromotions);
                totalOrderDiscount = baselineOrderDiscount;
                logger.LogDebug("Baseline order discounts won: {Count} promotions, total {Amount:C}",
                    baselineOrderPromotions.Count, totalOrderDiscount);
            }

            // Step 4: Calculate taxes with multiple tax entries support
            var taxResult = await CalculateTaxWithEntriesAsync(resultItems, totalPrice, totalOrderDiscount, productLookup, cancellationToken);
            totalTax = taxResult.TotalTax;
            taxEntries = taxResult.TaxEntries;

            // Update result items with tax info
            for (int i = 0; i < resultItems.Count; i++)
            {
                var item = resultItems[i];
                resultItems[i] = item with
                {
                    TaxRate = taxResult.EffectiveRate,
                    LineTax = Math.Round(item.LineSubtotal * taxResult.EffectiveRate, 2, MidpointRounding.AwayFromZero),
                    LineTotal = item.LineSubtotal + Math.Round(item.LineSubtotal * taxResult.EffectiveRate, 2, MidpointRounding.AwayFromZero)
                };
            }
        }

        // Step 5: Calculate shipping (Checkout mode only)
        if (request.Mode == PriceCalculationMode.Checkout && request.ShippingMethodId.HasValue)
        {
            shippingPrice = await CalculateShippingPriceAsync(request.ShippingMethodId.Value, cancellationToken);
        }

        var grandTotal = totalPrice - totalOrderDiscount + totalTax + shippingPrice;
        var defaultCurrency = await GetDefaultCurrencyCodeAsync();

        logger.LogDebug("Price calculation complete: GrandTotal={GrandTotal}, Tax={Tax}, Shipping={Shipping}",
            grandTotal, totalTax, shippingPrice);

        return new PriceCalculationResult
        {
            Items = resultItems,
            TotalPrice = totalPrice,
            TotalCatalogDiscount = totalCatalogDiscount,
            TotalOrderDiscount = totalOrderDiscount,
            TotalTax = totalTax,
            TaxRate = taxEntries.Count > 0 ? taxEntries.Sum(t => t.Rate) : 0m,
            ShippingPrice = shippingPrice,
            GrandTotal = grandTotal,
            CatalogPromotions = catalogPromotions,
            OrderPromotions = orderPromotions,
            Mode = request.Mode,
            Currency = defaultCurrency,
            TaxEntries = taxEntries
        };
    }

    /// <summary>
    /// Result from tax calculation with multiple tax entries.
    /// </summary>
    private record TaxWithEntriesResult(
        decimal TotalTax,
        decimal EffectiveRate,
        List<TaxEntryInfo> TaxEntries);

    /// <summary>
    /// Calculates taxes with support for multiple tax classes per product.
    /// Uses pre-loaded product lookup to avoid N+1 queries.
    /// </summary>
    private async Task<TaxWithEntriesResult> CalculateTaxWithEntriesAsync(
        List<PriceCalculationResultItem> items,
        decimal totalPrice,
        decimal totalOrderDiscount,
        Dictionary<int, object> productLookup,
        CancellationToken cancellationToken)
    {
        var taxEntries = new List<TaxEntryInfo>();
        var taxByClass = new Dictionary<int, (string Name, decimal Rate, decimal TaxableAmount)>();
        decimal subtotalForTax = 0m;

        // Pre-load default tax class once (avoid hitting DB per item with no tax classes)
        TaxClass? cachedDefaultTaxClass = null;
        bool defaultTaxClassLoaded = false;

        foreach (var item in items)
        {
            if (!int.TryParse(item.ProductIdentifier, out var productId) || productId <= 0)
            {
                // Skip virtual products like gift cards
                continue;
            }

            subtotalForTax += item.LineSubtotal;

            // Use pre-loaded product lookup instead of individual DB query
            if (!productLookup.TryGetValue(productId, out var product))
            {
                continue;
            }

            // Get tax classes from product via shared helper
            var taxClassGuids = ProductFieldHelper.GetTaxClassGuids(product);

            if (taxClassGuids.Count > 0)
            {
                // Apply each tax class from the product
                foreach (var taxGuid in taxClassGuids)
                {
                    var taxClass = await taxClassService.GetTaxClassByGuidAsync(taxGuid, cancellationToken);
                    if (taxClass != null && taxClass.DefaultRate > 0)
                    {
                        if (taxByClass.TryGetValue(taxClass.Id, out var existing))
                        {
                            taxByClass[taxClass.Id] = (existing.Name, existing.Rate, existing.TaxableAmount + item.LineSubtotal);
                        }
                        else
                        {
                            taxByClass[taxClass.Id] = (taxClass.DisplayName, taxClass.DefaultRate, item.LineSubtotal);
                        }
                    }
                }
            }
            else
            {
                // No specific tax classes - use default (loaded once)
                if (!defaultTaxClassLoaded)
                {
                    cachedDefaultTaxClass = await taxClassService.GetDefaultTaxClassAsync(cancellationToken);
                    defaultTaxClassLoaded = true;
                }

                if (cachedDefaultTaxClass != null && cachedDefaultTaxClass.DefaultRate > 0)
                {
                    if (taxByClass.TryGetValue(cachedDefaultTaxClass.Id, out var existing))
                    {
                        taxByClass[cachedDefaultTaxClass.Id] = (existing.Name, existing.Rate, existing.TaxableAmount + item.LineSubtotal);
                    }
                    else
                    {
                        taxByClass[cachedDefaultTaxClass.Id] = (cachedDefaultTaxClass.DisplayName, cachedDefaultTaxClass.DefaultRate, item.LineSubtotal);
                    }
                }
            }
        }

        // Calculate discount ratio
        decimal discountRatio = 0m;
        if (totalOrderDiscount > 0 && subtotalForTax > 0)
        {
            discountRatio = Math.Min(1m, totalOrderDiscount / subtotalForTax);
        }

        decimal totalTax = 0m;

        // Build tax entries
        foreach (var (id, (name, rate, taxableAmount)) in taxByClass)
        {
            var taxAmount = taxableAmount * rate * (1m - discountRatio);
            taxAmount = Math.Round(taxAmount, 2, MidpointRounding.AwayFromZero);
            totalTax += taxAmount;

            taxEntries.Add(new TaxEntryInfo(name, rate, taxAmount));
        }

        var effectiveRate = taxEntries.Count > 0 ? taxEntries.Sum(t => t.Rate) : 0m;

        return new TaxWithEntriesResult(totalTax, effectiveRate, taxEntries);
    }

    /// <summary>
    /// Calculates item price using pre-loaded product lookup (no DB call).
    /// The original <c>CalculateItemPriceAsync</c> is kept for callers outside <see cref="CalculateAsync"/>.
    /// </summary>
    private static PriceCalculationResultItem CalculateItemPrice(
        PriceCalculationRequestItem item,
        Dictionary<int, object> productLookup)
    {
        // Use override price for virtual products (e.g., gift cards)
        if (item.OverridePrice.HasValue)
        {
            var overrideLineSubtotal = item.OverridePrice.Value * item.Quantity;
            return new PriceCalculationResultItem
            {
                ProductIdentifier = item.ProductIdentifier,
                Quantity = item.Quantity,
                OriginalUnitPrice = item.OverridePrice.Value,
                UnitPrice = item.OverridePrice.Value,
                LineSubtotal = overrideLineSubtotal,
                LineTax = 0,
                LineTotal = overrideLineSubtotal,
                Discount = 0,
                TaxRate = 0
            };
        }

        decimal unitPrice = 0;
        if (int.TryParse(item.ProductIdentifier, out int productId)
            && productLookup.TryGetValue(productId, out var product))
        {
            unitPrice = ProductFieldHelper.GetPrice(product);
        }

        var lineSubtotal = unitPrice * item.Quantity;

        return new PriceCalculationResultItem
        {
            ProductIdentifier = item.ProductIdentifier,
            Quantity = item.Quantity,
            OriginalUnitPrice = unitPrice,
            UnitPrice = unitPrice,
            LineSubtotal = lineSubtotal,
            LineTax = 0,
            LineTotal = lineSubtotal,
            Discount = 0,
            TaxRate = 0
        };
    }

    private async Task<PriceCalculationResultItem> CalculateItemPriceAsync(
        PriceCalculationRequestItem item,
        CancellationToken cancellationToken)
    {
        // Use override price for virtual products (e.g., gift cards)
        if (item.OverridePrice.HasValue)
        {
            var overrideLineSubtotal = item.OverridePrice.Value * item.Quantity;
            return new PriceCalculationResultItem
            {
                ProductIdentifier = item.ProductIdentifier,
                Quantity = item.Quantity,
                OriginalUnitPrice = item.OverridePrice.Value,
                UnitPrice = item.OverridePrice.Value,
                LineSubtotal = overrideLineSubtotal,
                LineTax = 0,
                LineTotal = overrideLineSubtotal,
                Discount = 0,
                TaxRate = 0
            };
        }

        // Try to parse product ID from identifier
        decimal unitPrice = 0;
        if (int.TryParse(item.ProductIdentifier, out int productId))
        {
            var products = await productRepository.GetProductsByIdsAsync([productId]);
            var product = products.FirstOrDefault();
            if (product != null)
            {
                unitPrice = ProductFieldHelper.GetPrice(product);
            }
        }

        var lineSubtotal = unitPrice * item.Quantity;

        return new PriceCalculationResultItem
        {
            ProductIdentifier = item.ProductIdentifier,
            Quantity = item.Quantity,
            OriginalUnitPrice = unitPrice,
            UnitPrice = unitPrice,
            LineSubtotal = lineSubtotal,
            LineTax = 0,
            LineTotal = lineSubtotal,
            Discount = 0,
            TaxRate = 0
        };
    }

    private static (decimal discount, AppliedPromotion? applied) ApplyCatalogPromotion(
        CatalogPromotion promo,
        List<PriceCalculationResultItem> items)
    {
        // Catalog promotions apply to individual items
        // This is a simplified implementation - real logic would check conditions
        decimal totalDiscount = 0;

        if (promo.DiscountType == PromotionDiscountType.Percentage)
        {
            foreach (var item in items.ToList())
            {
                var discount = Math.Round(item.LineSubtotal * promo.DiscountValue / 100, 2);
                totalDiscount += discount;
                var index = items.IndexOf(item);
                items[index] = item with
                {
                    UnitPrice = item.UnitPrice - (discount / item.Quantity),
                    LineSubtotal = item.LineSubtotal - discount,
                    Discount = item.Discount + discount,
                    CatalogPromotion = new AppliedPromotion
                    {
                        PromotionId = promo.PromotionGuid.ToString(),
                        Name = promo.PromotionName,
                        DiscountAmount = discount,
                        IsCatalogPromotion = true
                    }
                };
            }
        }

        if (totalDiscount > 0)
        {
            return (totalDiscount, new AppliedPromotion
            {
                PromotionId = promo.PromotionGuid.ToString(),
                Name = promo.PromotionName,
                DiscountAmount = totalDiscount,
                IsCatalogPromotion = true
            });
        }

        return (0, null);
    }

    private static (decimal discount, AppliedPromotion? applied) ApplyOrderPromotion(
        OrderPromotion promo,
        decimal orderTotal,
        ICollection<string> couponCodes)
    {
        // Check if coupon is required and provided
        if (!string.IsNullOrEmpty(promo.CouponCode) && !couponCodes.Contains(promo.CouponCode))
        {
            return (0, null);
        }

        // Check minimum order value
        if (promo.MinimumRequirementValue.HasValue && orderTotal < promo.MinimumRequirementValue.Value)
        {
            return (0, null);
        }

        decimal discount = promo.DiscountType switch
        {
            PromotionDiscountType.Percentage => Math.Round(orderTotal * promo.DiscountValue / 100, 2),
            PromotionDiscountType.FixedAmount => Math.Min(promo.DiscountValue, orderTotal),
            _ => 0
        };

        if (discount > 0)
        {
            return (discount, new AppliedPromotion
            {
                PromotionId = promo.PromotionGuid.ToString(),
                Name = promo.PromotionName,
                DiscountAmount = discount,
                IsCatalogPromotion = false
            });
        }

        return (0, null);
    }

    private async Task<decimal> GetTaxRateAsync(Address? address)
    {
        // Get tax rate from CommerceChannelSettings first
        var settings = await GetChannelSettingsAsync();

        // Check region-specific rates from TaxCalculationOptions (for complex tax scenarios)
        if (address?.StateProvince != null &&
            _taxOptions.TaxRatesByRegion.TryGetValue(address.StateProvince, out var regionRate))
        {
            return regionRate;
        }

        // Check country-specific rates
        if (address?.CountryCode != null &&
            _taxOptions.TaxRatesByRegion.TryGetValue(address.CountryCode, out var countryRate))
        {
            return countryRate;
        }

        // Fall back to channel settings default tax rate (percentage stored as e.g., 14.975)
        if (settings?.DefaultTaxRate > 0)
        {
            return settings.DefaultTaxRate / 100m; // Convert percentage to decimal
        }

        // Ultimate fallback to options
        return _taxOptions.DefaultTaxRate;
    }

    private async Task<CommerceChannelSettings?> GetChannelSettingsAsync()
    {
        return await cache.GetOrCreateAsync(ChannelSettingsCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            entry.Size = 1;
            return await channelSettingsRepository.GetSettingsModel<CommerceChannelSettings>();
        });
    }

    /// <summary>
    /// Gets the default currency code from channel settings.
    /// </summary>
    private async Task<string> GetDefaultCurrencyCodeAsync()
    {
        var settings = await GetChannelSettingsAsync();
        return !string.IsNullOrEmpty(settings?.DefaultCurrency)
            ? settings.DefaultCurrency
            : _options.Pricing.DefaultCurrency;
    }

    /// <summary>
    /// Gets whether prices include tax from channel settings.
    /// </summary>
    public async Task<bool> GetPricesIncludeTaxAsync()
    {
        var settings = await GetChannelSettingsAsync();
        return settings?.PricesIncludeTax ?? _taxOptions.PricesIncludeTax;
    }

    /// <summary>
    /// Gets the default tax rate from channel settings.
    /// </summary>
    public async Task<decimal> GetDefaultTaxRateAsync()
    {
        var settings = await GetChannelSettingsAsync();
        // Channel settings stores as percentage (e.g., 14.975), convert to decimal (0.14975)
        return settings?.DefaultTaxRate > 0
            ? settings.DefaultTaxRate / 100m
            : _taxOptions.DefaultTaxRate;
    }

    private async Task<decimal> CalculateShippingPriceAsync(int shippingMethodId, CancellationToken cancellationToken)
    {
        try
        {
            var methods = await shippingMethodInfoProvider.Get()
                .WhereEquals(nameof(ShippingMethodInfo.ShippingMethodID), shippingMethodId)
                .TopN(1)
                .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

            var method = methods.FirstOrDefault();
            if (method != null)
            {
                logger.LogDebug("Found shipping method: {MethodName}", method.ShippingMethodDisplayName);
                // Note: Actual shipping cost calculation is site-specific
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to get shipping method: {ShippingMethodId}", shippingMethodId);
        }

        return _options.Shipping.DefaultFlatRate;
    }

    /// <inheritdoc/>
    public async Task<Money> CalculatePriceAsync(int productId, int quantity = 1)
    {
        logger.LogDebug("Calculating price: ProductId={ProductId}, Quantity={Quantity}", productId, quantity);

        try
        {
            var products = await productRepository.GetProductsByIdsAsync([productId]);
            var product = products.FirstOrDefault();

            if (product == null)
            {
                logger.LogWarning("Product not found for price calculation: {ProductId}", productId);
                return Money.Zero(_options.Pricing.DefaultCurrency);
            }

            var unitPrice = ProductFieldHelper.GetPrice(product);
            return new Money { Amount = unitPrice * quantity, Currency = _options.Pricing.DefaultCurrency };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to calculate price: ProductId={ProductId}", productId);
            return Money.Zero(_options.Pricing.DefaultCurrency);
        }
    }

    /// <inheritdoc/>
    public async Task<Money> CalculateTaxAsync(Money amount, Address? shippingAddress = null)
    {
        logger.LogDebug("Calculating tax for amount: {Amount}", amount.Amount);

        // Get tax rate from channel settings, with address-based overrides
        var taxRate = await GetTaxRateAsync(shippingAddress);
        var taxAmount = amount.Amount * taxRate;

        return new Money { Amount = taxAmount, Currency = amount.Currency };
    }

    /// <inheritdoc/>
    public async Task<Money> CalculateShippingAsync(Cart cart, Guid shippingMethodId)
    {
        logger.LogDebug("Calculating shipping: ShippingMethodId={ShippingMethodId}, ItemCount={ItemCount}", shippingMethodId, cart.ItemCount);

        if (cart.IsEmpty)
        {
            return Money.Zero(_options.Pricing.DefaultCurrency);
        }

        try
        {
            // Try to get shipping cost from Kentico shipping method
            var shippingMethods = await shippingMethodInfoProvider.Get()
                .WhereEquals(nameof(ShippingMethodInfo.ShippingMethodGUID), shippingMethodId)
                .GetEnumerableTypedResultAsync();

            var method = shippingMethods.FirstOrDefault();
            if (method != null)
            {
                // Note: ShippingMethodInfo doesn't have a direct price property
                // Shipping costs are typically calculated based on weight, destination, etc.
                // This is a placeholder - implement site-specific shipping logic
                logger.LogDebug("Found shipping method: {MethodName}", method.ShippingMethodDisplayName);
            }

            // Default flat rate shipping from options
            return new Money { Amount = _options.Shipping.DefaultFlatRate, Currency = _options.Pricing.DefaultCurrency };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to calculate shipping: ShippingMethodId={ShippingMethodId}", shippingMethodId);
            return new Money { Amount = _options.Shipping.DefaultFlatRate, Currency = _options.Pricing.DefaultCurrency };
        }
    }

    /// <inheritdoc/>
    public async Task<CartTotals> CalculateCartTotalsAsync(Cart cart)
    {
        logger.LogDebug("Calculating cart totals: ItemCount={ItemCount}", cart.ItemCount);

        if (cart.IsEmpty)
        {
            return new CartTotals
            {
                Subtotal = Money.Zero(_options.Pricing.DefaultCurrency),
                Tax = Money.Zero(_options.Pricing.DefaultCurrency),
                Shipping = Money.Zero(_options.Pricing.DefaultCurrency),
                Discount = Money.Zero(_options.Pricing.DefaultCurrency),
                Total = Money.Zero(_options.Pricing.DefaultCurrency)
            };
        }

        try
        {
            // Batch-load all products once to avoid N+1 queries
            var productIds = cart.Items.Select(i => i.ProductId).Distinct().ToArray();
            var products = await productRepository.GetProductsByIdsAsync(productIds);
            var productLookup = ProductFieldHelper.BuildLookup(products);

            // Calculate subtotal from cart items using pre-loaded products
            var subtotal = Money.Zero(_options.Pricing.DefaultCurrency);
            foreach (var item in cart.Items)
            {
                decimal unitPrice = productLookup.TryGetValue(item.ProductId, out var product)
                    ? ProductFieldHelper.GetPrice(product)
                    : 0m;
                subtotal += new Money { Amount = unitPrice * item.Quantity, Currency = _options.Pricing.DefaultCurrency };
            }

            // Calculate discounts
            var discount = Money.Zero(_options.Pricing.DefaultCurrency);
            foreach (var appliedDiscount in cart.Discounts)
            {
                discount += appliedDiscount.Amount;
            }

            // Calculate tax on subtotal after discounts
            var taxableAmount = subtotal - discount;
            if (taxableAmount.Amount < 0) taxableAmount = Money.Zero(_options.Pricing.DefaultCurrency);
            var tax = await CalculateTaxAsync(taxableAmount);

            // Calculate shipping - use method from cart if available
            var shipping = cart.Totals.Shipping.Amount > 0
                ? cart.Totals.Shipping
                : new Money { Amount = _options.Shipping.DefaultFlatRate, Currency = _options.Pricing.DefaultCurrency };

            // Calculate total
            var total = subtotal - discount + tax + shipping;

            return new CartTotals
            {
                Subtotal = subtotal,
                Discount = discount,
                Tax = tax,
                Shipping = shipping,
                Total = total
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to calculate cart totals");
            return new CartTotals
            {
                Subtotal = Money.Zero(_options.Pricing.DefaultCurrency),
                Tax = Money.Zero(_options.Pricing.DefaultCurrency),
                Shipping = Money.Zero(_options.Pricing.DefaultCurrency),
                Discount = Money.Zero(_options.Pricing.DefaultCurrency),
                Total = Money.Zero(_options.Pricing.DefaultCurrency)
            };
        }
    }

    /// <inheritdoc/>
    public async Task<string> FormatMoneyAsync(Money amount, CancellationToken cancellationToken = default)
    {
        // Use ICurrencyService for database-backed currency formatting
        var currency = await currencyService.GetCurrencyByCodeAsync(amount.Currency, cancellationToken);
        if (currency != null)
        {
            return currencyService.FormatAmount(amount.Amount, currency);
        }

        // Fallback to default currency
        var defaultCurrency = await currencyService.GetDefaultCurrencyAsync(cancellationToken);
        if (defaultCurrency != null)
        {
            return currencyService.FormatAmount(amount.Amount, defaultCurrency);
        }

        // Final fallback to culture-based formatting
        return amount.Amount.ToString("C", CurrencyCultureResolver.Resolve(amount.Currency));
    }

    /// <inheritdoc/>
    public string FormatMoney(Money amount)
    {
        // Synchronous version uses culture-based formatting as fallback
        // For database-backed currency formatting, use FormatMoneyAsync
        return amount.Amount.ToString("C", CurrencyCultureResolver.Resolve(amount.Currency));
    }

    #region Private Helpers

    /// <summary>
    /// Gets active catalog promotions with short-lived caching (2 minutes).
    /// Reduces database load during high-traffic price calculation scenarios.
    /// </summary>
    private async Task<IReadOnlyList<CatalogPromotion>> GetCachedCatalogPromotionsAsync(CancellationToken cancellationToken)
    {
        return await cache.GetOrCreateAsync(CatalogPromotionsCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = PromotionCacheExpiry;
            entry.Size = 1;
            logger.LogDebug("Loading catalog promotions from database (cache miss)");
            return await promotionService.GetActiveCatalogPromotionsAsync(cancellationToken);
        }) ?? [];
    }

    /// <summary>
    /// Gets active order promotions with short-lived caching (2 minutes).
    /// Reduces database load during high-traffic price calculation scenarios.
    /// </summary>
    private async Task<IReadOnlyList<OrderPromotion>> GetCachedOrderPromotionsAsync(CancellationToken cancellationToken)
    {
        return await cache.GetOrCreateAsync(OrderPromotionsCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = PromotionCacheExpiry;
            entry.Size = 1;
            logger.LogDebug("Loading order promotions from database (cache miss)");
            return await promotionService.GetActiveOrderPromotionsAsync(cancellationToken);
        }) ?? [];
    }

    #endregion
}
