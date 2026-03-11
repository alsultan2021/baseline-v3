using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Baseline.Ecommerce.Admin.ViewModels;

/// <summary>
/// View model for Shipping Promotion create/edit forms.
/// </summary>
public class ShippingPromotionViewModel
{
    public int PromotionID { get; set; }

    public Guid PromotionGuid { get; set; } = Guid.NewGuid();

    [TextInputComponent(
        Label = "Code Name",
        ExplanationText = "Unique identifier (e.g., free-shipping-over-50)",
        WatermarkText = "free-shipping-over-50",
        Order = 1)]
    [RequiredValidationRule(ErrorMessage = "Code Name is required")]
    public string PromotionName { get; set; } = string.Empty;

    [TextInputComponent(
        Label = "Display Name",
        ExplanationText = "Name shown to customers",
        WatermarkText = "Free Shipping on Orders Over $50",
        Order = 2)]
    [RequiredValidationRule(ErrorMessage = "Display Name is required")]
    public string PromotionDisplayName { get; set; } = string.Empty;

    [TextAreaComponent(
        Label = "Description",
        ExplanationText = "Internal description of the shipping promotion",
        Order = 3)]
    public string PromotionDescription { get; set; } = string.Empty;

    [DropDownComponent(
        Label = "Shipping Discount Type",
        ExplanationText = "Free Shipping removes all shipping cost. Reduced Rate is a percentage off. Flat Rate overrides the shipping cost.",
        DataProviderType = typeof(ShippingDiscountTypeDataProvider),
        Order = 4)]
    public string ShippingDiscountType { get; set; } = "0";

    [DecimalNumberInputComponent(
        Label = "Discount Value",
        ExplanationText = "For reduced rate: percentage (e.g., 50 for 50% off). For flat rate: the flat shipping cost (e.g., 4.99). Ignored for free shipping.",
        Order = 5)]
    public decimal PromotionDiscountValue { get; set; } = 0m;

    [DecimalNumberInputComponent(
        Label = "Max Shipping Discount",
        ExplanationText = "Optional cap on the shipping discount amount. Leave empty for no cap.",
        Order = 6)]
    public decimal? MaxShippingDiscount { get; set; }

    [DropDownComponent(
        Label = "Minimum Requirement",
        ExplanationText = "Require a minimum purchase amount or quantity for this shipping promotion",
        DataProviderType = typeof(MinimumRequirementTypeDataProvider),
        Order = 7)]
    public string MinimumRequirementType { get; set; } = "0";

    [DecimalNumberInputComponent(
        Label = "Minimum Value",
        ExplanationText = "Minimum amount or quantity required",
        Order = 8)]
    public decimal? MinimumRequirementValue { get; set; }

    [TextAreaComponent(
        Label = "Target Shipping Zones",
        ExplanationText = "Comma-separated zone codes. Leave empty to apply to all zones.",
        Order = 9)]
    public string TargetShippingZones { get; set; } = string.Empty;

    [TextAreaComponent(
        Label = "Target Categories",
        ExplanationText = "Comma-separated category codes. Leave empty to apply to all products.",
        Order = 10)]
    public string TargetCategories { get; set; } = string.Empty;

    [DateTimeInputComponent(
        Label = "Active From",
        ExplanationText = "When the promotion becomes active",
        Order = 11)]
    [RequiredValidationRule(ErrorMessage = "Active From date is required")]
    public DateTime PromotionActiveFrom { get; set; } = DateTime.UtcNow;

    [DateTimeInputComponent(
        Label = "Active To",
        ExplanationText = "When the promotion expires (leave empty for no end date)",
        Order = 12)]
    public DateTime? PromotionActiveTo { get; set; }

    [CheckBoxComponent(
        Label = "Enabled",
        ExplanationText = "Uncheck to deactivate the promotion",
        Order = 13)]
    public bool PromotionEnabled { get; set; } = true;

    [NumberInputComponent(
        Label = "Order",
        ExplanationText = "Display order in listings",
        Order = 20)]
    public int PromotionOrder { get; set; } = 0;
}
