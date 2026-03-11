using CMS.Commerce;
using CMS.DataEngine;
using CMS.Websites.Routing;
using Ecommerce.Extensions;
using Ecommerce.Services;
using Kentico.Commerce.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EcommerceServices = Ecommerce.Services;
using EcommerceModels = Ecommerce.Models;

namespace Baseline.Ecommerce;

/// <summary>
/// v3 implementation of IPriceCalculationService for Xperience by Kentico.
/// Provides price calculation functionality for shopping cart and checkout.
/// Integrates with IProductRepository for product price lookups.
/// Supports per-item tax calculation for digital product exemptions.
/// Also integrates with Kentico Digital Commerce for order promotions.
/// </summary>
public sealed class PriceCalculationService(
    ICurrentShoppingCartRetriever cartRetriever,
    IProductRepository productRepository,
    IInfoProvider<ShippingMethodInfo> shippingMethodInfoProvider,
    ITaxClassService taxClassService,
    IPromotionService promotionService,
    KenticoDcPromotionHelper dcPromotionHelper,
    IPreferredLanguageRetriever languageRetriever,
    IOptions<TaxCalculationOptions> taxOptions,
    ILogger<PriceCalculationService> logger) : EcommerceServices.IPriceCalculationService
{
    /// <inheritdoc/>
    public async Task<decimal> CalculateTotalAsync(
        EcommerceModels.ShoppingCartDataModel cart,
        CancellationToken cancellationToken = default)
    {
        if (cart.Items.Count == 0)
        {
            return 0m;
        }

        // Get all product IDs from cart
        var productIds = cart.Items.Select(i => i.ContentItemId).Distinct().ToList();

        // Fetch product prices using the repository
        var priceLookup = await productRepository.GetProductPricesAsync(productIds, cancellationToken);

        // Calculate total by summing (quantity * unit price) for each item
        decimal total = 0m;
        foreach (var item in cart.Items)
        {
            // Handle gift cards (virtual products with ContentItemId = -1)
            if (item.ContentItemId == -1 && item.Options?.ContainsKey("IsGiftCard") == true)
            {
                var amount = decimal.TryParse(item.Options.GetValueOrDefault("Amount", "0"), out var a) ? a : 0m;
                total += item.Quantity * amount;
                continue;
            }

            if (priceLookup.TryGetValue(item.ContentItemId, out var unitPrice))
            {
                // Check for variant-specific pricing
                if (item.VariantId.HasValue)
                {
                    var variantPrice = await productRepository.GetVariantPriceAsync(
                        item.ContentItemId, item.VariantId.Value, cancellationToken);
                    if (variantPrice.HasValue)
                    {
                        unitPrice = variantPrice.Value;
                    }
                }

                total += item.Quantity * unitPrice;
            }
            else
            {
                logger.LogWarning("Product {ProductId} not found for price calculation", item.ContentItemId);
            }
        }

        logger.LogDebug("Calculated cart subtotal: {Total:C} for {ItemCount} items", total, cart.Items.Count);
        return total;
    }

    /// <inheritdoc/>
    public async Task<decimal> CalculateTaxAsync(
        EcommerceModels.ShoppingCartDataModel cart,
        CancellationToken cancellationToken = default)
    {
        // Get the effective tax rate from database or config
        var taxRate = await GetEffectiveTaxRateAsync(cancellationToken);

        if (taxRate == 0m)
        {
            return 0m;
        }

        // Calculate subtotal using product repository prices
        var subtotal = await CalculateTotalAsync(cart, cancellationToken);
        var tax = subtotal * taxRate;

        logger.LogDebug("Calculated tax {TaxAmount:C} at rate {TaxRate:P2}", tax, taxRate);

        return Math.Round(tax, 2, MidpointRounding.AwayFromZero);
    }

    /// <inheritdoc/>
    public async Task<decimal> CalculateShippingAsync(
        EcommerceModels.ShoppingCartDataModel cart,
        int? shippingMethodId = null,
        CancellationToken cancellationToken = default)
    {
        if (!shippingMethodId.HasValue || shippingMethodId.Value <= 0)
        {
            return 0m;
        }

        try
        {
            var shippingMethod = await shippingMethodInfoProvider.GetAsync(shippingMethodId.Value, cancellationToken);
            if (shippingMethod != null)
            {
                logger.LogDebug("Shipping method {Name} has price {Price:C}",
                    shippingMethod.ShippingMethodDisplayName, shippingMethod.ShippingMethodPrice);
                return shippingMethod.ShippingMethodPrice;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to retrieve shipping method {Id}", shippingMethodId);
        }

        return 0m;
    }

    /// <inheritdoc/>
    Task<decimal> EcommerceServices.IPriceCalculationService.CalculateShippingAsync(
        EcommerceModels.ShoppingCartDataModel cart,
        CancellationToken cancellationToken)
        => CalculateShippingAsync(cart, null, cancellationToken);

    /// <inheritdoc/>
    public async Task<EcommerceServices.PriceCalculationResult> CalculateCartAsync(
        int? shippingMethodId,
        int? paymentMethodId,
        EcommerceModels.OrderAddress? shippingAddress,
        CancellationToken cancellationToken = default)
    {
        var cart = await cartRetriever.Get(cancellationToken);
        if (cart == null)
        {
            return new EcommerceServices.PriceCalculationResult
            {
                Subtotal = 0,
                Tax = 0,
                Shipping = 0,
                Discount = 0,
                GrandTotal = 0,
                TaxRate = 0,
                TaxName = "Tax"
            };
        }

        // Get cart data from the ShoppingCartInfo using extension method
        var cartData = cart.GetShoppingCartDataModel();

        // Calculate subtotal using product repository for accurate pricing
        decimal subtotal = await CalculateTotalAsync(cartData, cancellationToken);

        // Separate gift card discounts from promo discounts
        // Gift cards are a form of payment, not a discount - they should NOT reduce taxable amount
        decimal giftCardDiscount = 0;
        decimal promoDiscount = 0;
        string? discountDescription = null;
        var discountDescriptions = new List<string>();
        var discountEntries = new List<EcommerceServices.DiscountEntryResult>();

        // Load persisted discounts from cart data model (gift cards, coupons)
        // Skip KenticoDcCoupon — those are evaluated by the DC promotion engine below
        if (cartData.AppliedDiscounts?.Count > 0)
        {
            foreach (var d in cartData.AppliedDiscounts)
            {
                if (d.DiscountType == nameof(DiscountType.KenticoDcCoupon))
                {
                    continue;
                }

                // Use description if available, otherwise format based on type
                var name = !string.IsNullOrWhiteSpace(d.Description)
                    ? d.Description
                    : d.DiscountType == nameof(DiscountType.GiftCard)
                        ? $"Gift Card ({d.Code})"
                        : !string.IsNullOrWhiteSpace(d.Code)
                            ? d.Code
                            : "Discount";
                discountDescriptions.Add(name);
                discountEntries.Add(new EcommerceServices.DiscountEntryResult { Name = name, Amount = d.Amount });

                // Gift cards don't reduce taxable amount - they're applied after tax
                if (d.DiscountType == nameof(DiscountType.GiftCard))
                {
                    giftCardDiscount += d.Amount;
                }
                else
                {
                    promoDiscount += d.Amount;
                }
            }
        }

        // Evaluate BOTH promotion engines and pick the better discount for each type.
        var dcItems = cartData.Items
            .Select(i => new KenticoDcPromotionHelper.LineItem(i.ContentItemId, i.Quantity))
            .ToList();
        var language = languageRetriever.Get();

        // 1. Catalog promotions — check both, pick better
        var catalogDiscount = await dcPromotionHelper.GetCatalogPromotionsAsync(
            dcItems, cartData.CouponCodes, customerId: 0, language, cancellationToken);
        decimal dcCatalogAmount = catalogDiscount?.TotalDiscount ?? 0;

        var baselineCatalog = await promotionService.CalculateBestCatalogDiscountAsync(
            cartData.Items.FirstOrDefault()?.ContentItemId ?? 0, subtotal);
        decimal baselineCatalogAmount = baselineCatalog.HasDiscount ? baselineCatalog.DiscountAmount : 0;

        if (dcCatalogAmount >= baselineCatalogAmount && dcCatalogAmount > 0)
        {
            promoDiscount += dcCatalogAmount;
            discountDescriptions.Add(catalogDiscount!.Value.Name);
            discountEntries.Add(new EcommerceServices.DiscountEntryResult { Name = catalogDiscount.Value.Name, Amount = dcCatalogAmount });
            logger.LogDebug("Kentico DC catalog discount won: {Name} = {Amount:C}",
                catalogDiscount.Value.Name, dcCatalogAmount);
        }
        else if (baselineCatalogAmount > 0)
        {
            promoDiscount += baselineCatalogAmount;
            var label = baselineCatalog.DiscountLabel ?? "Catalog Discount";
            discountDescriptions.Add(label);
            discountEntries.Add(new EcommerceServices.DiscountEntryResult { Name = label, Amount = baselineCatalogAmount });
            logger.LogDebug("Baseline catalog discount won: {Label} = {Amount:C}", label, baselineCatalogAmount);
        }

        // 2. Order promotions — check both, pick better
        var kenticoDcDiscount = await dcPromotionHelper.GetOrderPromotionAsync(
            dcItems, cartData.CouponCodes, customerId: 0, language, cancellationToken);
        decimal dcOrderAmount = kenticoDcDiscount?.Amount ?? 0;

        var baselineOrder = await promotionService.CalculateBestOrderDiscountAsync(
            subtotal, cartData.Items.Sum(i => i.Quantity), null, null);
        decimal baselineOrderAmount = baselineOrder.HasDiscount ? baselineOrder.DiscountAmount : 0;

        if (dcOrderAmount >= baselineOrderAmount && dcOrderAmount > 0)
        {
            promoDiscount += dcOrderAmount;
            discountDescriptions.Add(kenticoDcDiscount!.Value.Name);
            discountEntries.Add(new EcommerceServices.DiscountEntryResult { Name = kenticoDcDiscount.Value.Name, Amount = dcOrderAmount });
            logger.LogDebug("Kentico DC order discount won: {Name} = {Amount:C}",
                kenticoDcDiscount.Value.Name, dcOrderAmount);
        }
        else if (baselineOrderAmount > 0)
        {
            promoDiscount += baselineOrderAmount;
            var label = baselineOrder.DiscountLabel ?? "Discount";
            discountDescriptions.Add(label);
            discountEntries.Add(new EcommerceServices.DiscountEntryResult { Name = label, Amount = baselineOrderAmount });
            logger.LogDebug("Baseline order discount won: {Label} = {Amount:C}", label, baselineOrderAmount);
        }

        discountDescription = discountDescriptions.Count > 0 ? string.Join(", ", discountDescriptions) : null;

        // Get effective tax rate and name from config/database
        var (defaultTaxRate, defaultTaxName) = await GetEffectiveTaxInfoAsync(cancellationToken);

        // Calculate tax per-item with multiple tax entries support
        // IMPORTANT: Only promo discounts reduce taxable amount, NOT gift cards
        var taxResult = await CalculatePerItemTaxWithEntriesAsync(
            cartData, defaultTaxRate, defaultTaxName, promoDiscount, cancellationToken);

        // Calculate shipping based on selected method
        var shipping = await CalculateShippingAsync(cartData, shippingMethodId, cancellationToken);

        // Total discount for display (includes both promo and gift card)
        decimal totalDiscount = promoDiscount + giftCardDiscount;

        // Grand total: (subtotal - promo discounts) + tax + shipping - gift card
        // Gift card is applied AFTER tax calculation
        decimal grandTotal = subtotal - promoDiscount + taxResult.TotalTax + shipping - giftCardDiscount;
        if (grandTotal < 0) grandTotal = 0;

        return new EcommerceServices.PriceCalculationResult
        {
            Subtotal = subtotal,
            Tax = taxResult.TotalTax,
            Shipping = shipping,
            Discount = totalDiscount,
            DiscountDescription = discountDescription,
            DiscountEntries = discountEntries,
            TaxEntries = taxResult.TaxEntries,
            GrandTotal = grandTotal,
            TaxRate = taxResult.EffectiveRate,
            TaxName = taxResult.EffectiveName
        };
    }

    /// <summary>
    /// Gets the effective tax rate and name, first from database, then from configuration.
    /// </summary>
    private async Task<(decimal Rate, string Name)> GetEffectiveTaxInfoAsync(CancellationToken cancellationToken = default)
    {
        // First, try to get from database tax class
        if (taxOptions.Value.UseDatabaseTaxClasses)
        {
            try
            {
                var defaultTaxClass = await taxClassService.GetDefaultTaxClassAsync(cancellationToken);
                if (defaultTaxClass != null)
                {
                    logger.LogDebug("Using database default tax class: {Name} at {Rate:P2}", defaultTaxClass.DisplayName, defaultTaxClass.DefaultRate);
                    return (defaultTaxClass.DefaultRate, defaultTaxClass.DisplayName);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to get default tax class from database, falling back to config");
            }
        }

        // Fall back to configuration
        return (taxOptions.Value.DefaultTaxRate, "Tax");
    }

    /// <summary>
    /// Gets the effective tax rate, first from database, then from configuration.
    /// </summary>
    private async Task<decimal> GetEffectiveTaxRateAsync(CancellationToken cancellationToken = default)
    {
        var (rate, _) = await GetEffectiveTaxInfoAsync(cancellationToken);
        return rate;
    }

    /// <summary>
    /// Result from tax calculation with multiple tax entries.
    /// </summary>
    private record TaxCalculationResult(
        decimal TotalTax,
        decimal EffectiveRate,
        string EffectiveName,
        IList<EcommerceServices.TaxEntryResult> TaxEntries);

    /// <summary>
    /// Calculates tax per-item with support for multiple tax classes per product.
    /// Returns separate entries for each tax type (e.g., GST, QST).
    /// </summary>
    private async Task<TaxCalculationResult> CalculatePerItemTaxWithEntriesAsync(
        EcommerceModels.ShoppingCartDataModel cart,
        decimal defaultTaxRate,
        string defaultTaxName,
        decimal totalDiscount,
        CancellationToken cancellationToken)
    {
        var taxEntries = new List<EcommerceServices.TaxEntryResult>();
        var exemptTaxName = await GetTaxExemptClassNameAsync(cancellationToken);

        if (cart.Items.Count == 0)
        {
            return new TaxCalculationResult(0m, 0m, exemptTaxName, taxEntries);
        }

        // Get all product IDs and fetch products
        var productIds = cart.Items
            .Where(i => i.ContentItemId > 0)
            .Select(i => i.ContentItemId)
            .Distinct()
            .ToList();

        var products = await productRepository.GetProductsByIdsAsync(productIds, cancellationToken);
        var productLookup = new Dictionary<int, object>();
        var priceLookup = await productRepository.GetProductPricesAsync(productIds, cancellationToken);

        foreach (var product in products)
        {
            if (product is CMS.ContentEngine.IContentItemFieldsSource contentItem)
            {
                productLookup[contentItem.SystemFields.ContentItemID] = product;
            }
        }

        // Track tax by tax class for aggregation
        var taxByClass = new Dictionary<Guid, (string Name, decimal Rate, decimal TaxableAmount)>();
        decimal subtotalForTax = 0m;
        bool hasDefaultTax = false;
        decimal defaultTaxableAmount = 0m;

        foreach (var item in cart.Items)
        {
            // Handle gift cards (virtual products with ContentItemId = -1)
            if (item.ContentItemId == -1 && item.Options?.ContainsKey("IsGiftCard") == true)
            {
                continue;
            }

            if (!priceLookup.TryGetValue(item.ContentItemId, out var unitPrice))
            {
                continue;
            }

            var lineTotal = item.Quantity * unitPrice;
            subtotalForTax += lineTotal;

            // Check if product has assigned tax classes
            var productTaxGuids = productLookup.TryGetValue(item.ContentItemId, out var product)
                ? ProductFieldHelper.GetTaxClassGuids(product)
                : [];

            // If product has specific tax classes, apply each of them
            if (productTaxGuids.Count > 0)
            {
                foreach (var taxGuid in productTaxGuids)
                {
                    var taxClass = await taxClassService.GetTaxClassByGuidAsync(taxGuid, cancellationToken);
                    if (taxClass != null && taxClass.DefaultRate > 0)
                    {
                        if (taxByClass.TryGetValue(taxGuid, out var existing))
                        {
                            taxByClass[taxGuid] = (existing.Name, existing.Rate, existing.TaxableAmount + lineTotal);
                        }
                        else
                        {
                            taxByClass[taxGuid] = (taxClass.DisplayName, taxClass.DefaultRate, lineTotal);
                        }
                    }
                }
            }
            else if (defaultTaxRate > 0)
            {
                // No specific tax classes - use default
                hasDefaultTax = true;
                defaultTaxableAmount += lineTotal;
            }
        }

        // Calculate discount ratio for proportional reduction
        decimal discountRatio = 0m;
        if (totalDiscount > 0 && subtotalForTax > 0)
        {
            discountRatio = Math.Min(1m, totalDiscount / subtotalForTax);
        }

        decimal totalTax = 0m;

        // Build tax entries for product-specific taxes
        foreach (var (guid, (name, rate, taxableAmount)) in taxByClass)
        {
            var taxAmount = taxableAmount * rate * (1m - discountRatio);
            taxAmount = Math.Round(taxAmount, 2, MidpointRounding.AwayFromZero);
            totalTax += taxAmount;

            taxEntries.Add(new EcommerceServices.TaxEntryResult
            {
                Name = name,
                Rate = rate,
                Amount = taxAmount
            });
        }

        // Add default tax entry if applicable
        if (hasDefaultTax && defaultTaxRate > 0)
        {
            var defaultTaxAmount = defaultTaxableAmount * defaultTaxRate * (1m - discountRatio);
            defaultTaxAmount = Math.Round(defaultTaxAmount, 2, MidpointRounding.AwayFromZero);
            totalTax += defaultTaxAmount;

            taxEntries.Add(new EcommerceServices.TaxEntryResult
            {
                Name = defaultTaxName,
                Rate = defaultTaxRate,
                Amount = defaultTaxAmount
            });
        }

        // Determine effective rate and name
        decimal effectiveRate;
        string effectiveName;

        if (taxEntries.Count == 0 || totalTax == 0m)
        {
            effectiveRate = 0m;
            effectiveName = exemptTaxName;
        }
        else if (taxEntries.Count == 1)
        {
            effectiveRate = taxEntries[0].Rate;
            effectiveName = taxEntries[0].Name;
        }
        else
        {
            // Multiple taxes - combined rate and name
            effectiveRate = taxEntries.Sum(t => t.Rate);
            effectiveName = string.Join(" + ", taxEntries.Select(t => t.Name));
        }

        return new TaxCalculationResult(totalTax, effectiveRate, effectiveName, taxEntries);
    }

    /// <summary>
    /// Gets the tax-exempt class name from database, with fallback to config or default.
    /// </summary>
    private async Task<string> GetTaxExemptClassNameAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to find a tax class with 0% rate (tax exempt)
            var taxClasses = await taxClassService.GetTaxClassesAsync(cancellationToken);
            var exemptClass = taxClasses.FirstOrDefault(tc => tc.DefaultRate == 0m);

            if (exemptClass != null)
            {
                return exemptClass.DisplayName;
            }

            // Check TaxRatesByCategory for "Digital" or "TaxExempt" category name
            if (taxOptions.Value.TaxRatesByCategory.TryGetValue("Digital", out var digitalRate) && digitalRate == 0m)
            {
                return "Digital (Tax Exempt)";
            }

            if (taxOptions.Value.TaxRatesByCategory.TryGetValue("TaxExempt", out var exemptRate) && exemptRate == 0m)
            {
                return "Tax Exempt";
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to get tax exempt class name from database");
        }

        return "Tax Exempt";
    }
}
