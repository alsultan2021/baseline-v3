using CMS.ContentEngine;
using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Baseline.Ecommerce.Admin.ViewModels;

/// <summary>
/// View model for Coupon create/edit forms.
/// </summary>
public class CouponViewModel
{
    /// <summary>
    /// Coupon ID (hidden on create).
    /// </summary>
    public int CouponID { get; set; }

    /// <summary>
    /// Coupon GUID.
    /// </summary>
    public Guid CouponGuid { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Coupon code.
    /// </summary>
    [TextInputComponent(
        Label = "Coupon Code",
        ExplanationText = "The code customers will enter at checkout (will be converted to uppercase)",
        WatermarkText = "SUMMER25",
        Order = 1)]
    [RequiredValidationRule(ErrorMessage = "Coupon Code is required")]
    public string CouponCode { get; set; } = string.Empty;

    /// <summary>
    /// Associated promotion ID.
    /// </summary>
    [DropDownComponent(
        Label = "Promotion",
        ExplanationText = "The promotion to apply when this coupon is used",
        DataProviderType = typeof(PromotionDataProvider),
        Order = 2)]
    [RequiredValidationRule(ErrorMessage = "Promotion is required")]
    public int CouponPromotionID { get; set; }

    /// <summary>
    /// Coupon type.
    /// </summary>
    [DropDownComponent(
        Label = "Coupon Type",
        ExplanationText = "Single-use: one redemption per customer. Multi-use: limited total redemptions. Unlimited: no limit.",
        DataProviderType = typeof(CouponTypeDataProvider),
        Order = 3)]
    public int CouponType { get; set; } = 0;

    /// <summary>
    /// Maximum number of redemptions.
    /// </summary>
    [NumberInputComponent(
        Label = "Usage Limit",
        ExplanationText = "Maximum number of times this coupon can be used (leave empty for unlimited)",
        Order = 4)]
    public int? CouponUsageLimit { get; set; }

    /// <summary>
    /// Coupon expiration date.
    /// </summary>
    [DateTimeInputComponent(
        Label = "Expiration Date",
        ExplanationText = "When this coupon expires (leave empty for no expiration)",
        Order = 5)]
    public DateTime? CouponExpirationDate { get; set; }

    /// <summary>
    /// Whether the coupon is enabled.
    /// </summary>
    [CheckBoxComponent(
        Label = "Enabled",
        ExplanationText = "Uncheck to disable this coupon",
        Order = 6)]
    public bool CouponEnabled { get; set; } = true;
}

/// <summary>
/// View model for Product Stock create/edit forms.
/// </summary>
public class ProductStockViewModel
{
    /// <summary>
    /// Stock ID (hidden on create).
    /// </summary>
    public int ProductStockID { get; set; }

    /// <summary>
    /// Stock GUID.
    /// </summary>
    public Guid ProductStockGuid { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Content item reference linking to the product.
    /// Uses ContentItemSelectorComponent for proper Admin UI product selection.
    /// </summary>
    [ContentItemSelectorComponent(
        typeof(ProductFieldsSchemaFilter),
        Label = "Product",
        ExplanationText = "Select the product to track stock for",
        MaximumItems = 1,
        MinimumItems = 1,
        Order = 1)]
    [RequiredValidationRule(ErrorMessage = "Product selection is required")]
    public IEnumerable<ContentItemReference> ProductStockProduct { get; set; } = [];

    /// <summary>
    /// Available quantity.
    /// </summary>
    [DecimalNumberInputComponent(
        Label = "Available Quantity",
        ExplanationText = "Current quantity available for purchase",
        Order = 2)]
    public decimal ProductStockAvailableQuantity { get; set; } = 0m;

    /// <summary>
    /// Reserved quantity.
    /// </summary>
    [DecimalNumberInputComponent(
        Label = "Reserved Quantity",
        ExplanationText = "Quantity reserved for pending orders",
        Order = 3)]
    public decimal ProductStockReservedQuantity { get; set; } = 0m;

    /// <summary>
    /// Minimum threshold for low stock warning.
    /// </summary>
    [DecimalNumberInputComponent(
        Label = "Minimum Threshold",
        ExplanationText = "Alert when stock falls below this quantity",
        Order = 4)]
    public decimal? ProductStockMinimumThreshold { get; set; }

    /// <summary>
    /// Whether backorders are allowed.
    /// </summary>
    [CheckBoxComponent(
        Label = "Allow Backorders",
        ExplanationText = "Allow customers to order even when out of stock",
        Order = 5)]
    public bool ProductStockAllowBackorders { get; set; }

    /// <summary>
    /// Whether stock tracking is enabled.
    /// </summary>
    [CheckBoxComponent(
        Label = "Track Inventory",
        ExplanationText = "Enable inventory tracking for this product",
        Order = 6)]
    public bool ProductStockTrackingEnabled { get; set; } = true;
}

/// <summary>
/// Filter for content item selector to only show content types with ProductFields reusable schema.
/// </summary>
public class ProductFieldsSchemaFilter : IReusableFieldSchemasFilter
{
    /// <summary>
    /// Allowed schema names - filters to ProductFields schema.
    /// </summary>
    public IEnumerable<string> AllowedSchemaNames => ["ProductFields"];
}
