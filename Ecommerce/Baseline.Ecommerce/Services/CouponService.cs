using Baseline.Ecommerce.Models;
using CMS.DataEngine;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Baseline.Ecommerce.Services;

/// <summary>
/// Service for managing coupon codes.
/// </summary>
public class CouponService(
    IInfoProvider<CouponInfo> couponProvider,
    IInfoProvider<PromotionInfo> promotionProvider,
    IMemoryCache cache,
    ILogger<CouponService> logger) : ICouponService
{
    private readonly IInfoProvider<CouponInfo> couponProvider = couponProvider;
    private readonly IInfoProvider<PromotionInfo> promotionProvider = promotionProvider;
    private readonly IMemoryCache cache = cache;
    private readonly ILogger<CouponService> logger = logger;

    /// <inheritdoc/>
    public async Task<PromotionCouponValidationResult> ValidateAsync(string couponCode)
    {
        if (string.IsNullOrWhiteSpace(couponCode))
        {
            return new PromotionCouponValidationResult(false, "Coupon code is required.", null, null, null);
        }

        var coupon = await GetByCodeAsync(couponCode);

        if (coupon is null)
        {
            return new PromotionCouponValidationResult(false, "Invalid coupon code.", null, null, null);
        }

        if (!coupon.IsActive)
        {
            return new PromotionCouponValidationResult(false, "This coupon is no longer valid.", coupon, null, null);
        }

        if (coupon.ExpirationDate.HasValue && DateTime.UtcNow > coupon.ExpirationDate.Value)
        {
            return new PromotionCouponValidationResult(false, "This coupon has expired.", coupon, null, null);
        }

        if (coupon.UsageLimit.HasValue && coupon.UsageCount >= coupon.UsageLimit.Value)
        {
            return new PromotionCouponValidationResult(false, "This coupon has reached its usage limit.", coupon, null, null);
        }

        return new PromotionCouponValidationResult(true, null, coupon, null, null);
    }

    /// <inheritdoc/>
    public async Task<PromotionCoupon?> GetByCodeAsync(string couponCode)
    {
        if (string.IsNullOrWhiteSpace(couponCode))
        {
            return null;
        }

        var normalizedCode = couponCode.Trim().ToUpperInvariant();
        var cacheKey = $"baseline.ecommerce.coupon.{normalizedCode}";

        return await cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            entry.Size = 1;

            var coupons = await couponProvider
                .Get()
                .WhereEquals(nameof(CouponInfo.CouponCode), normalizedCode)
                .TopN(1)
                .GetEnumerableTypedResultAsync();

            var couponInfo = coupons.FirstOrDefault();
            return couponInfo is null ? null : MapToCoupon(couponInfo);
        });
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<PromotionCoupon>> GetCouponsForPromotionAsync(int promotionId)
    {
        var coupons = await couponProvider
            .Get()
            .WhereEquals(nameof(CouponInfo.CouponPromotionID), promotionId)
            .GetEnumerableTypedResultAsync();

        return coupons.Select(MapToCoupon).ToList().AsReadOnly();
    }

    /// <inheritdoc/>
    public async Task<CouponRedemptionResult> RecordRedemptionAsync(string couponCode, Guid orderId)
    {
        if (string.IsNullOrWhiteSpace(couponCode))
        {
            return new CouponRedemptionResult(false, "Coupon code is required.", 0);
        }

        var normalizedCode = couponCode.Trim().ToUpperInvariant();

        var coupons = await couponProvider
            .Get()
            .WhereEquals(nameof(CouponInfo.CouponCode), normalizedCode)
            .TopN(1)
            .GetEnumerableTypedResultAsync();

        var couponInfo = coupons.FirstOrDefault();

        if (couponInfo is null)
        {
            return new CouponRedemptionResult(false, "Invalid coupon code.", 0);
        }

        // Check if already at limit
        if (couponInfo.HasReachedUsageLimit())
        {
            return new CouponRedemptionResult(false, "This coupon has reached its usage limit.", couponInfo.CouponUsageCount);
        }

        // Increment usage count
        couponInfo.CouponUsageCount++;
        await couponProvider.SetAsync(couponInfo);

        // Also increment promotion redemption count
        var promotions = await promotionProvider
            .Get()
            .WhereEquals(nameof(PromotionInfo.PromotionID), couponInfo.CouponPromotionID)
            .TopN(1)
            .GetEnumerableTypedResultAsync();

        var promotionInfo = promotions.FirstOrDefault();
        if (promotionInfo is not null)
        {
            promotionInfo.PromotionRedemptionCount++;
            await promotionProvider.SetAsync(promotionInfo);
        }

        // Clear cache
        cache.Remove($"baseline.ecommerce.coupon.{normalizedCode}");

        logger.LogInformation("Recorded redemption for coupon {CouponCode} on order {OrderId}. New usage count: {UsageCount}",
            couponCode, orderId, couponInfo.CouponUsageCount);

        return new CouponRedemptionResult(true, null, couponInfo.CouponUsageCount);
    }

    /// <inheritdoc/>
    public async Task<int> GetRedemptionCountAsync(string couponCode)
    {
        var coupon = await GetByCodeAsync(couponCode);
        return coupon?.UsageCount ?? 0;
    }

    /// <inheritdoc/>
    public async Task<bool> HasReachedUsageLimitAsync(string couponCode)
    {
        var coupon = await GetByCodeAsync(couponCode);

        if (coupon is null)
        {
            return true; // Invalid coupon treated as exhausted
        }

        return coupon.UsageLimit.HasValue && coupon.UsageCount >= coupon.UsageLimit.Value;
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
}
