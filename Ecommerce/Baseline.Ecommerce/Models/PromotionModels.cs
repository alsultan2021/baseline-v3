namespace Baseline.Ecommerce;

/// <summary>
/// Represents a catalog promotion (discount applied to individual products).
/// </summary>
public record CatalogPromotion(
    int PromotionId,
    Guid PromotionGuid,
    string PromotionName,
    string PromotionDisplayName,
    string? PromotionDescription,
    PromotionDiscountType DiscountType,
    decimal DiscountValue,
    DateTime ActiveFrom,
    DateTime? ActiveTo,
    bool IsActive,
    PromotionStatus Status,
    string? CouponCode,
    IReadOnlyList<string> TargetCategories,
    IReadOnlyList<int> TargetProductIds,
    string RuleType,
    string? RulePropertiesJson,
    DateTime CreatedDate,
    DateTime LastModified);

/// <summary>
/// Represents an order promotion (discount applied to entire order).
/// </summary>
public record OrderPromotion(
    int PromotionId,
    Guid PromotionGuid,
    string PromotionName,
    string PromotionDisplayName,
    string? PromotionDescription,
    PromotionDiscountType DiscountType,
    decimal DiscountValue,
    DateTime ActiveFrom,
    DateTime? ActiveTo,
    bool IsActive,
    PromotionStatus Status,
    string? CouponCode,
    MinimumRequirementType MinimumRequirementType,
    decimal? MinimumRequirementValue,
    string RuleType,
    string? RulePropertiesJson,
    DateTime CreatedDate,
    DateTime LastModified);

/// <summary>
/// Represents a shipping promotion (free or discounted shipping).
/// </summary>
public record ShippingPromotion(
    int PromotionId,
    Guid PromotionGuid,
    string PromotionName,
    string PromotionDisplayName,
    string? PromotionDescription,
    ShippingDiscountType ShippingDiscountType,
    decimal DiscountValue,
    decimal? MaxShippingDiscount,
    DateTime ActiveFrom,
    DateTime? ActiveTo,
    bool IsActive,
    PromotionStatus Status,
    string? CouponCode,
    MinimumRequirementType MinimumRequirementType,
    decimal? MinimumRequirementValue,
    IReadOnlyList<string> TargetShippingZones,
    IReadOnlyList<string> TargetCategories,
    IReadOnlyList<int> TargetProductIds,
    DateTime CreatedDate,
    DateTime LastModified);

/// <summary>
/// Represents a Buy X Get Y promotion.
/// </summary>
public record BuyXGetYPromotion(
    int PromotionId,
    Guid PromotionGuid,
    string PromotionName,
    string PromotionDisplayName,
    string? PromotionDescription,
    int BuyQuantity,
    int GetQuantity,
    decimal GetDiscountPercentage,
    DateTime ActiveFrom,
    DateTime? ActiveTo,
    bool IsActive,
    PromotionStatus Status,
    string? CouponCode,
    IReadOnlyList<string> TargetCategories,
    IReadOnlyList<int> TargetProductIds,
    string RuleType,
    string? RulePropertiesJson,
    DateTime CreatedDate,
    DateTime LastModified);

/// <summary>
/// Result of a shipping discount calculation.
/// </summary>
public record ShippingDiscountResult(
    bool HasDiscount,
    decimal OriginalShippingCost,
    decimal DiscountAmount,
    decimal DiscountedShippingCost,
    int? AppliedPromotionId,
    string? PromotionName,
    string? DiscountLabel);

/// <summary>
/// Result of a Buy X Get Y discount calculation.
/// </summary>
public record BuyXGetYDiscountResult(
    bool HasDiscount,
    decimal OriginalTotal,
    decimal DiscountAmount,
    decimal DiscountedTotal,
    int FreeItemCount,
    int? AppliedPromotionId,
    string? PromotionName,
    string? DiscountLabel);

/// <summary>
/// Represents a promotion coupon code.
/// </summary>
public record PromotionCoupon(
    int CouponId,
    Guid CouponGuid,
    string CouponCode,
    int PromotionId,
    CouponType CouponType,
    int? UsageLimit,
    int UsageCount,
    DateTime? ExpirationDate,
    bool IsActive);

/// <summary>
/// Represents product stock information.
/// </summary>
public record ProductStock(
    int StockId,
    Guid ProductGuid,
    decimal AvailableQuantity,
    decimal ReservedQuantity,
    decimal? MinimumThreshold,
    bool AllowBackorders,
    StockStatus Status,
    DateTime LastUpdated);

/// <summary>
/// Result of a catalog discount calculation.
/// </summary>
public record CatalogDiscountResult(
    bool HasDiscount,
    decimal OriginalPrice,
    decimal DiscountAmount,
    decimal DiscountedPrice,
    int? AppliedPromotionId,
    string? PromotionName,
    string? DiscountLabel);

/// <summary>
/// Result of an order discount calculation.
/// </summary>
public record OrderDiscountResult(
    bool HasDiscount,
    decimal OrderSubtotal,
    decimal DiscountAmount,
    decimal DiscountedTotal,
    int? AppliedPromotionId,
    string? PromotionName,
    string? DiscountLabel);

/// <summary>
/// Result of promotion coupon validation.
/// </summary>
public record PromotionCouponValidationResult(
    bool IsValid,
    string? ErrorMessage,
    PromotionCoupon? Coupon,
    CatalogPromotion? CatalogPromotion,
    OrderPromotion? OrderPromotion);

/// <summary>
/// Result of applying a coupon.
/// </summary>
public record CouponApplicationResult(
    bool Success,
    string? ErrorMessage,
    string? CouponCode,
    decimal? DiscountAmount);

/// <summary>
/// Result of coupon redemption.
/// </summary>
public record CouponRedemptionResult(
    bool Success,
    string? ErrorMessage,
    int NewUsageCount);

/// <summary>
/// Result of a stock reservation operation.
/// </summary>
public record StockReservationResult(
    bool Success,
    string? ErrorMessage,
    Guid? ReservationId,
    decimal? ReservedQuantity);

/// <summary>
/// Result of a stock update operation.
/// </summary>
public record StockUpdateResult(
    bool Success,
    string? ErrorMessage,
    decimal? PreviousQuantity,
    decimal? NewQuantity);

/// <summary>
/// Discount type for promotions.
/// </summary>
public enum PromotionDiscountType
{
    /// <summary>Percentage discount (e.g., 10% off).</summary>
    Percentage = 0,

    /// <summary>Fixed amount discount (e.g., $5 off).</summary>
    FixedAmount = 1
}

/// <summary>
/// Promotion status.
/// </summary>
public enum PromotionStatus
{
    /// <summary>Promotion is scheduled but not yet active.</summary>
    Scheduled = 0,

    /// <summary>Promotion is currently active.</summary>
    Active = 1,

    /// <summary>Promotion has been deactivated.</summary>
    Deactivated = 2,

    /// <summary>Promotion has expired.</summary>
    Expired = 3
}

/// <summary>
/// Minimum requirement type for order promotions.
/// </summary>
public enum MinimumRequirementType
{
    /// <summary>No minimum requirement.</summary>
    None = 0,

    /// <summary>Minimum purchase amount required.</summary>
    MinimumPurchaseAmount = 1,

    /// <summary>Minimum quantity of items required.</summary>
    MinimumQuantity = 2
}

/// <summary>
/// Type of coupon.
/// </summary>
public enum CouponType
{
    /// <summary>Single-use coupon (one redemption per customer).</summary>
    SingleUse = 0,

    /// <summary>Multi-use coupon (limited total redemptions).</summary>
    MultiUse = 1,

    /// <summary>Unlimited coupon (no redemption limit).</summary>
    Unlimited = 2
}

/// <summary>
/// Product stock status.
/// </summary>
public enum StockStatus
{
    /// <summary>Product is in stock.</summary>
    InStock = 0,

    /// <summary>Product has low stock (below threshold).</summary>
    LowStock = 1,

    /// <summary>Product is out of stock.</summary>
    OutOfStock = 2,

    /// <summary>Product allows backorders.</summary>
    Backorder = 3
}

/// <summary>
/// Shipping discount type for shipping promotions.
/// </summary>
public enum ShippingDiscountType
{
    /// <summary>Completely free shipping.</summary>
    FreeShipping = 0,

    /// <summary>Percentage reduction on shipping cost.</summary>
    ReducedRate = 1,

    /// <summary>Flat-rate override (e.g., ship for $5 regardless).</summary>
    FlatRate = 2
}
