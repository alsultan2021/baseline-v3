using System.Text.Json;
using Baseline.Ecommerce.Models;
using CMS.DataEngine;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Baseline.Ecommerce.Services;

/// <summary>
/// Service for managing promotions stored in the custom PromotionInfo table.
/// Used directly by display components (listing active promotions) and
/// as the Baseline engine in PricingService/PriceCalculationService which
/// evaluate both DC and Baseline promotions and pick the better discount.
/// </summary>
public class PromotionService(
    IInfoProvider<PromotionInfo> promotionProvider,
    IInfoProvider<CouponInfo> couponProvider,
    IMemoryCache cache,
    ILogger<PromotionService> logger) : IPromotionService
{
    private readonly IInfoProvider<PromotionInfo> promotionProvider = promotionProvider;
    private readonly IInfoProvider<CouponInfo> couponProvider = couponProvider;
    private readonly IMemoryCache cache = cache;
    private readonly ILogger<PromotionService> logger = logger;

    private const string CatalogPromotionsCacheKey = "baseline.ecommerce.catalogpromotions.active";
    private const string OrderPromotionsCacheKey = "baseline.ecommerce.orderpromotions.active";
    private const string ShippingPromotionsCacheKey = "baseline.ecommerce.shippingpromotions.active";
    private const string BuyXGetYPromotionsCacheKey = "baseline.ecommerce.buyxgetypromotions.active";
    private static readonly TimeSpan CacheExpiry = TimeSpan.FromMinutes(15);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<CatalogPromotion>> GetActiveCatalogPromotionsAsync(CancellationToken cancellationToken = default)
    {
        return await cache.GetOrCreateAsync(CatalogPromotionsCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheExpiry;
            entry.Size = 1;

            var now = DateTime.UtcNow;
            var promotions = await promotionProvider
                .Get()
                .WhereEquals(nameof(PromotionInfo.PromotionType), (int)PromotionTypeEnum.Catalog)
                .WhereEquals(nameof(PromotionInfo.PromotionEnabled), true)
                .WhereLessOrEquals(nameof(PromotionInfo.PromotionActiveFrom), now)
                .Where(w => w
                    .WhereNull(nameof(PromotionInfo.PromotionActiveTo))
                    .Or()
                    .WhereGreaterOrEquals(nameof(PromotionInfo.PromotionActiveTo), now))
                .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

            return promotions.Select(MapToCatalogPromotion).ToList().AsReadOnly();
        }) ?? [];
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<OrderPromotion>> GetActiveOrderPromotionsAsync(CancellationToken cancellationToken = default)
    {
        return await cache.GetOrCreateAsync(OrderPromotionsCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheExpiry;
            entry.Size = 1;

            var now = DateTime.UtcNow;

            var promotions = await promotionProvider
                .Get()
                .WhereEquals(nameof(PromotionInfo.PromotionType), (int)PromotionTypeEnum.Order)
                .WhereEquals(nameof(PromotionInfo.PromotionEnabled), true)
                .WhereLessOrEquals(nameof(PromotionInfo.PromotionActiveFrom), now)
                .Where(w => w
                    .WhereNull(nameof(PromotionInfo.PromotionActiveTo))
                    .Or()
                    .WhereGreaterOrEquals(nameof(PromotionInfo.PromotionActiveTo), now))
                .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

            return promotions.Select(MapToOrderPromotion).ToList().AsReadOnly();
        }) ?? [];
    }

    /// <inheritdoc/>
    public async Task<CatalogPromotion?> GetCatalogPromotionByIdAsync(int promotionId)
    {
        var promotion = await promotionProvider
            .Get()
            .WhereEquals(nameof(PromotionInfo.PromotionID), promotionId)
            .WhereEquals(nameof(PromotionInfo.PromotionType), (int)PromotionTypeEnum.Catalog)
            .TopN(1)
            .GetEnumerableTypedResultAsync();

        var info = promotion.FirstOrDefault();
        return info is null ? null : MapToCatalogPromotion(info);
    }

    /// <inheritdoc/>
    public async Task<OrderPromotion?> GetOrderPromotionByIdAsync(int promotionId)
    {
        var promotion = await promotionProvider
            .Get()
            .WhereEquals(nameof(PromotionInfo.PromotionID), promotionId)
            .WhereEquals(nameof(PromotionInfo.PromotionType), (int)PromotionTypeEnum.Order)
            .TopN(1)
            .GetEnumerableTypedResultAsync();

        var info = promotion.FirstOrDefault();
        return info is null ? null : MapToOrderPromotion(info);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<CatalogPromotion>> GetPromotionsForProductAsync(int contentItemId)
    {
        var activePromotions = await GetActiveCatalogPromotionsAsync();

        // Filter promotions that target this specific product
        return activePromotions
            .Where(p => p.TargetProductIds.Contains(contentItemId) || !p.TargetProductIds.Any())
            .ToList()
            .AsReadOnly();
    }

    /// <inheritdoc/>
    public async Task<PromotionCouponValidationResult> ValidateCouponAsync(string couponCode)
    {
        if (string.IsNullOrWhiteSpace(couponCode))
        {
            return new PromotionCouponValidationResult(false, "Coupon code is required.", null, null, null);
        }

        var coupons = await couponProvider
            .Get()
            .WhereEquals(nameof(CouponInfo.CouponCode), couponCode.Trim().ToUpperInvariant())
            .GetEnumerableTypedResultAsync();

        var couponInfo = coupons.FirstOrDefault();

        if (couponInfo is null)
        {
            return new PromotionCouponValidationResult(false, "Invalid coupon code.", null, null, null);
        }

        if (!couponInfo.IsValid())
        {
            if (!couponInfo.CouponEnabled)
            {
                return new PromotionCouponValidationResult(false, "This coupon is no longer valid.", null, null, null);
            }

            if (couponInfo.CouponExpirationDate.HasValue && DateTime.UtcNow > couponInfo.CouponExpirationDate.Value)
            {
                return new PromotionCouponValidationResult(false, "This coupon has expired.", null, null, null);
            }

            if (couponInfo.HasReachedUsageLimit())
            {
                return new PromotionCouponValidationResult(false, "This coupon has reached its usage limit.", null, null, null);
            }
        }

        // Get the associated promotion
        var promotions = await promotionProvider
            .Get()
            .WhereEquals(nameof(PromotionInfo.PromotionID), couponInfo.CouponPromotionID)
            .GetEnumerableTypedResultAsync();

        var promotionInfo = promotions.FirstOrDefault();

        if (promotionInfo is null || !promotionInfo.IsCurrentlyActive())
        {
            return new PromotionCouponValidationResult(false, "The promotion associated with this coupon is not active.", null, null, null);
        }

        var coupon = MapToCoupon(couponInfo);

        return promotionInfo.PromotionType == (int)PromotionTypeEnum.Catalog
            ? new PromotionCouponValidationResult(true, null, coupon, MapToCatalogPromotion(promotionInfo), null)
            : promotionInfo.PromotionType == (int)PromotionTypeEnum.Order
                ? new PromotionCouponValidationResult(true, null, coupon, null, MapToOrderPromotion(promotionInfo))
                : new PromotionCouponValidationResult(true, null, coupon, null, null);
    }

    /// <inheritdoc/>
    public async Task<CouponApplicationResult> ApplyCouponAsync(string couponCode, Guid checkoutSessionId)
    {
        var validation = await ValidateCouponAsync(couponCode);

        if (!validation.IsValid)
        {
            return new CouponApplicationResult(false, validation.ErrorMessage, couponCode, null);
        }

        // Calculate discount amount based on promotion
        decimal discountAmount = 0m;
        if (validation.CatalogPromotion is not null)
        {
            // For catalog promotions, the discount is applied per product
            discountAmount = validation.CatalogPromotion.DiscountValue;
        }
        else if (validation.OrderPromotion is not null)
        {
            discountAmount = validation.OrderPromotion.DiscountValue;
        }

        logger.LogInformation("Applied coupon {CouponCode} to session {SessionId}", couponCode, checkoutSessionId);

        return new CouponApplicationResult(true, null, couponCode, discountAmount);
    }

    /// <inheritdoc/>
    public Task<CouponApplicationResult> RemoveCouponAsync(string couponCode, Guid checkoutSessionId)
    {
        logger.LogInformation("Removed coupon {CouponCode} from session {SessionId}", couponCode, checkoutSessionId);
        return Task.FromResult(new CouponApplicationResult(true, null, couponCode, null));
    }

    /// <inheritdoc/>
    public async Task<CatalogDiscountResult> CalculateBestCatalogDiscountAsync(
        int contentItemId,
        decimal unitPrice,
        string? taxCategory = null,
        IEnumerable<string>? productCategories = null)
    {
        var categories = productCategories?.ToList() ?? [];
        var promotions = await GetActiveCatalogPromotionsAsync();

        CatalogPromotion? bestPromotion = null;
        decimal bestDiscount = 0m;

        foreach (var promotion in promotions)
        {
            // Check if promotion applies to this product
            bool applies = false;

            // Check specific product targeting
            if (promotion.TargetProductIds.Contains(contentItemId))
            {
                applies = true;
            }
            // Check category targeting
            else if (promotion.TargetCategories.Any() && categories.Any())
            {
                applies = promotion.TargetCategories.Intersect(categories).Any();
            }
            // No targeting = applies to all
            else if (!promotion.TargetProductIds.Any() && !promotion.TargetCategories.Any())
            {
                applies = true;
            }

            if (!applies)
            {
                continue;
            }

            // Calculate discount
            var discount = promotion.DiscountType == PromotionDiscountType.Percentage
                ? Math.Round(unitPrice * (promotion.DiscountValue / 100m), 2)
                : Math.Min(promotion.DiscountValue, unitPrice);

            if (discount > bestDiscount)
            {
                bestDiscount = discount;
                bestPromotion = promotion;
            }
        }

        if (bestPromotion is null)
        {
            return new CatalogDiscountResult(false, unitPrice, 0m, unitPrice, null, null, null);
        }

        var label = bestPromotion.DiscountType == PromotionDiscountType.Percentage
            ? $"{bestPromotion.DiscountValue}% off"
            : $"${bestPromotion.DiscountValue:F2} off";

        return new CatalogDiscountResult(
            true,
            unitPrice,
            bestDiscount,
            unitPrice - bestDiscount,
            bestPromotion.PromotionId,
            bestPromotion.PromotionDisplayName,
            label);
    }

    /// <inheritdoc/>
    public async Task<OrderDiscountResult> CalculateBestOrderDiscountAsync(
        decimal orderSubtotal,
        int itemCount,
        Guid? customerId = null,
        IEnumerable<string>? appliedCoupons = null)
    {
        var promotions = await GetActiveOrderPromotionsAsync();

        OrderPromotion? bestPromotion = null;
        decimal bestDiscount = 0m;

        foreach (var promotion in promotions)
        {
            // Check minimum requirements
            if (!MeetsMinimumRequirement(promotion, orderSubtotal, itemCount))
            {
                continue;
            }

            // Calculate discount
            var discount = promotion.DiscountType == PromotionDiscountType.Percentage
                ? Math.Round(orderSubtotal * (promotion.DiscountValue / 100m), 2)
                : Math.Min(promotion.DiscountValue, orderSubtotal);

            if (discount > bestDiscount)
            {
                bestDiscount = discount;
                bestPromotion = promotion;
            }
        }

        if (bestPromotion is null)
        {
            return new OrderDiscountResult(false, orderSubtotal, 0m, orderSubtotal, null, null, null);
        }

        var label = bestPromotion.DiscountType == PromotionDiscountType.Percentage
            ? $"{bestPromotion.DiscountValue}% off"
            : $"${bestPromotion.DiscountValue:F2} off";

        return new OrderDiscountResult(
            true,
            orderSubtotal,
            bestDiscount,
            orderSubtotal - bestDiscount,
            bestPromotion.PromotionId,
            bestPromotion.PromotionDisplayName,
            label);
    }

    private static bool MeetsMinimumRequirement(OrderPromotion promotion, decimal orderSubtotal, int itemCount)
    {
        return promotion.MinimumRequirementType switch
        {
            MinimumRequirementType.None => true,
            MinimumRequirementType.MinimumPurchaseAmount =>
                !promotion.MinimumRequirementValue.HasValue || orderSubtotal >= promotion.MinimumRequirementValue.Value,
            MinimumRequirementType.MinimumQuantity =>
                !promotion.MinimumRequirementValue.HasValue || itemCount >= (int)promotion.MinimumRequirementValue.Value,
            _ => true
        };
    }

    private static bool MeetsMinimumRequirement(ShippingPromotion promotion, decimal orderSubtotal, int itemCount)
    {
        return promotion.MinimumRequirementType switch
        {
            MinimumRequirementType.None => true,
            MinimumRequirementType.MinimumPurchaseAmount =>
                !promotion.MinimumRequirementValue.HasValue || orderSubtotal >= promotion.MinimumRequirementValue.Value,
            MinimumRequirementType.MinimumQuantity =>
                !promotion.MinimumRequirementValue.HasValue || itemCount >= (int)promotion.MinimumRequirementValue.Value,
            _ => true
        };
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ShippingPromotion>> GetActiveShippingPromotionsAsync(CancellationToken cancellationToken = default)
    {
        return await cache.GetOrCreateAsync(ShippingPromotionsCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheExpiry;
            entry.Size = 1;

            var now = DateTime.UtcNow;
            var promotions = await promotionProvider
                .Get()
                .WhereEquals(nameof(PromotionInfo.PromotionType), (int)PromotionTypeEnum.Shipping)
                .WhereEquals(nameof(PromotionInfo.PromotionEnabled), true)
                .WhereLessOrEquals(nameof(PromotionInfo.PromotionActiveFrom), now)
                .Where(w => w
                    .WhereNull(nameof(PromotionInfo.PromotionActiveTo))
                    .Or()
                    .WhereGreaterOrEquals(nameof(PromotionInfo.PromotionActiveTo), now))
                .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

            return promotions.Select(MapToShippingPromotion).ToList().AsReadOnly();
        }) ?? [];
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<BuyXGetYPromotion>> GetActiveBuyXGetYPromotionsAsync(CancellationToken cancellationToken = default)
    {
        return await cache.GetOrCreateAsync(BuyXGetYPromotionsCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheExpiry;
            entry.Size = 1;

            var now = DateTime.UtcNow;
            var promotions = await promotionProvider
                .Get()
                .WhereEquals(nameof(PromotionInfo.PromotionType), (int)PromotionTypeEnum.BuyXGetY)
                .WhereEquals(nameof(PromotionInfo.PromotionEnabled), true)
                .WhereLessOrEquals(nameof(PromotionInfo.PromotionActiveFrom), now)
                .Where(w => w
                    .WhereNull(nameof(PromotionInfo.PromotionActiveTo))
                    .Or()
                    .WhereGreaterOrEquals(nameof(PromotionInfo.PromotionActiveTo), now))
                .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

            return promotions.Select(MapToBuyXGetYPromotion).ToList().AsReadOnly();
        }) ?? [];
    }

    /// <inheritdoc/>
    public async Task<ShippingPromotion?> GetShippingPromotionByIdAsync(int promotionId)
    {
        var promotion = await promotionProvider
            .Get()
            .WhereEquals(nameof(PromotionInfo.PromotionID), promotionId)
            .WhereEquals(nameof(PromotionInfo.PromotionType), (int)PromotionTypeEnum.Shipping)
            .TopN(1)
            .GetEnumerableTypedResultAsync();

        var info = promotion.FirstOrDefault();
        return info is null ? null : MapToShippingPromotion(info);
    }

    /// <inheritdoc/>
    public async Task<BuyXGetYPromotion?> GetBuyXGetYPromotionByIdAsync(int promotionId)
    {
        var promotion = await promotionProvider
            .Get()
            .WhereEquals(nameof(PromotionInfo.PromotionID), promotionId)
            .WhereEquals(nameof(PromotionInfo.PromotionType), (int)PromotionTypeEnum.BuyXGetY)
            .TopN(1)
            .GetEnumerableTypedResultAsync();

        var info = promotion.FirstOrDefault();
        return info is null ? null : MapToBuyXGetYPromotion(info);
    }

    /// <inheritdoc/>
    public async Task<ShippingDiscountResult> CalculateBestShippingDiscountAsync(
        decimal shippingCost,
        decimal orderSubtotal,
        int itemCount,
        string? shippingZone = null)
    {
        var promotions = await GetActiveShippingPromotionsAsync();

        ShippingPromotion? bestPromotion = null;
        decimal bestDiscount = 0m;

        foreach (var promotion in promotions)
        {
            if (!MeetsMinimumRequirement(promotion, orderSubtotal, itemCount))
            {
                continue;
            }

            // Check zone targeting
            if (promotion.TargetShippingZones.Any() && shippingZone is not null
                && !promotion.TargetShippingZones.Contains(shippingZone, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            var discount = promotion.ShippingDiscountType switch
            {
                ShippingDiscountType.FreeShipping => shippingCost,
                ShippingDiscountType.ReducedRate => Math.Round(shippingCost * (promotion.DiscountValue / 100m), 2),
                ShippingDiscountType.FlatRate => Math.Max(0m, shippingCost - promotion.DiscountValue),
                _ => 0m
            };

            if (promotion.MaxShippingDiscount.HasValue)
            {
                discount = Math.Min(discount, promotion.MaxShippingDiscount.Value);
            }

            discount = Math.Min(discount, shippingCost);

            if (discount > bestDiscount)
            {
                bestDiscount = discount;
                bestPromotion = promotion;
            }
        }

        if (bestPromotion is null)
        {
            return new ShippingDiscountResult(false, shippingCost, 0m, shippingCost, null, null, null);
        }

        string label = bestPromotion.ShippingDiscountType switch
        {
            ShippingDiscountType.FreeShipping => "Free shipping",
            ShippingDiscountType.ReducedRate => $"{bestPromotion.DiscountValue}% off shipping",
            ShippingDiscountType.FlatRate => $"Flat rate ${bestPromotion.DiscountValue:F2} shipping",
            _ => "Shipping discount"
        };

        return new ShippingDiscountResult(
            true,
            shippingCost,
            bestDiscount,
            shippingCost - bestDiscount,
            bestPromotion.PromotionId,
            bestPromotion.PromotionDisplayName,
            label);
    }

    /// <inheritdoc/>
    public async Task<BuyXGetYDiscountResult> CalculateBestBuyXGetYDiscountAsync(
        int contentItemId,
        decimal unitPrice,
        int quantity,
        IEnumerable<string>? productCategories = null)
    {
        var categories = productCategories?.ToList() ?? [];
        var promotions = await GetActiveBuyXGetYPromotionsAsync();

        BuyXGetYPromotion? bestPromotion = null;
        decimal bestDiscount = 0m;
        int bestFreeItems = 0;

        foreach (var promotion in promotions)
        {
            bool applies = false;

            if (promotion.TargetProductIds.Contains(contentItemId))
            {
                applies = true;
            }
            else if (promotion.TargetCategories.Any() && categories.Any())
            {
                applies = promotion.TargetCategories.Intersect(categories).Any();
            }
            else if (!promotion.TargetProductIds.Any() && !promotion.TargetCategories.Any())
            {
                applies = true;
            }

            if (!applies)
            {
                continue;
            }

            int cycleSize = promotion.BuyQuantity + promotion.GetQuantity;

            if (cycleSize <= 0 || quantity < cycleSize)
            {
                continue;
            }

            int fullCycles = quantity / cycleSize;
            int freeItems = fullCycles * promotion.GetQuantity;
            decimal discountPerItem = Math.Round(unitPrice * (promotion.GetDiscountPercentage / 100m), 2);
            decimal totalDiscount = freeItems * discountPerItem;

            if (totalDiscount > bestDiscount)
            {
                bestDiscount = totalDiscount;
                bestFreeItems = freeItems;
                bestPromotion = promotion;
            }
        }

        if (bestPromotion is null)
        {
            decimal total = unitPrice * quantity;
            return new BuyXGetYDiscountResult(false, total, 0m, total, 0, null, null, null);
        }

        decimal originalTotal = unitPrice * quantity;
        string label = bestPromotion.GetDiscountPercentage >= 100m
            ? $"Buy {bestPromotion.BuyQuantity} Get {bestPromotion.GetQuantity} Free"
            : $"Buy {bestPromotion.BuyQuantity} Get {bestPromotion.GetQuantity} at {bestPromotion.GetDiscountPercentage}% off";

        return new BuyXGetYDiscountResult(
            true,
            originalTotal,
            bestDiscount,
            originalTotal - bestDiscount,
            bestFreeItems,
            bestPromotion.PromotionId,
            bestPromotion.PromotionDisplayName,
            label);
    }

    private static CatalogPromotion MapToCatalogPromotion(PromotionInfo info)
    {
        var targetCategories = ParseJsonArray(info.PromotionTargetCategories);
        var targetProductIds = ParseIntJsonArray(info.PromotionTargetProducts);

        return new CatalogPromotion(
            info.PromotionID,
            info.PromotionGuid,
            info.PromotionName,
            info.PromotionDisplayName,
            string.IsNullOrEmpty(info.PromotionDescription) ? null : info.PromotionDescription,
            (PromotionDiscountType)info.PromotionDiscountType,
            info.PromotionDiscountValue,
            info.PromotionActiveFrom,
            info.PromotionActiveTo,
            info.IsCurrentlyActive(),
            info.GetStatus(),
            null,
            targetCategories,
            targetProductIds,
            info.PromotionRuleType,
            string.IsNullOrEmpty(info.PromotionRuleProperties) ? null : info.PromotionRuleProperties,
            info.PromotionCreated,
            info.PromotionLastModified);
    }

    private static OrderPromotion MapToOrderPromotion(PromotionInfo info)
    {
        return new OrderPromotion(
            info.PromotionID,
            info.PromotionGuid,
            info.PromotionName,
            info.PromotionDisplayName,
            string.IsNullOrEmpty(info.PromotionDescription) ? null : info.PromotionDescription,
            (PromotionDiscountType)info.PromotionDiscountType,
            info.PromotionDiscountValue,
            info.PromotionActiveFrom,
            info.PromotionActiveTo,
            info.IsCurrentlyActive(),
            info.GetStatus(),
            null, // Coupon loaded separately
            (MinimumRequirementType)info.PromotionMinimumRequirementType,
            info.PromotionMinimumRequirementValue,
            info.PromotionRuleType,
            string.IsNullOrEmpty(info.PromotionRuleProperties) ? null : info.PromotionRuleProperties,
            info.PromotionCreated,
            info.PromotionLastModified);
    }

    private static ShippingPromotion MapToShippingPromotion(PromotionInfo info)
    {
        var targetCategories = ParseJsonArray(info.PromotionTargetCategories);
        var targetProductIds = ParseIntJsonArray(info.PromotionTargetProducts);
        var targetZones = ParseJsonArray(info.PromotionTargetShippingZones);

        return new ShippingPromotion(
            info.PromotionID,
            info.PromotionGuid,
            info.PromotionName,
            info.PromotionDisplayName,
            string.IsNullOrEmpty(info.PromotionDescription) ? null : info.PromotionDescription,
            (ShippingDiscountType)info.PromotionShippingDiscountType,
            info.PromotionDiscountValue,
            info.PromotionMaxShippingDiscount,
            info.PromotionActiveFrom,
            info.PromotionActiveTo,
            info.IsCurrentlyActive(),
            info.GetStatus(),
            null, // Coupon loaded separately
            (MinimumRequirementType)info.PromotionMinimumRequirementType,
            info.PromotionMinimumRequirementValue,
            targetZones,
            targetCategories,
            targetProductIds,
            info.PromotionCreated,
            info.PromotionLastModified);
    }

    private static BuyXGetYPromotion MapToBuyXGetYPromotion(PromotionInfo info)
    {
        var targetCategories = ParseJsonArray(info.PromotionTargetCategories);
        var targetProductIds = ParseIntJsonArray(info.PromotionTargetProducts);

        return new BuyXGetYPromotion(
            info.PromotionID,
            info.PromotionGuid,
            info.PromotionName,
            info.PromotionDisplayName,
            string.IsNullOrEmpty(info.PromotionDescription) ? null : info.PromotionDescription,
            info.PromotionBuyQuantity,
            info.PromotionGetQuantity,
            info.PromotionGetDiscountPercentage,
            info.PromotionActiveFrom,
            info.PromotionActiveTo,
            info.IsCurrentlyActive(),
            info.GetStatus(),
            null, // Coupon loaded separately
            targetCategories,
            targetProductIds,
            info.PromotionRuleType,
            string.IsNullOrEmpty(info.PromotionRuleProperties) ? null : info.PromotionRuleProperties,
            info.PromotionCreated,
            info.PromotionLastModified);
    }

    private static PromotionCoupon MapToCoupon(CouponInfo info)
    {
        return new PromotionCoupon(
            info.CouponID,
            info.CouponGuid,
            info.CouponCode,
            info.CouponPromotionID,
            (CouponType)info.CouponType,
            info.CouponUsageLimit,
            info.CouponUsageCount,
            info.CouponExpirationDate,
            info.CouponEnabled && info.IsValid());
    }

    private static IReadOnlyList<string> ParseJsonArray(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static IReadOnlyList<int> ParseIntJsonArray(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<int>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }
}
