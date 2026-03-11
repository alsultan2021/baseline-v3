using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Baseline.Ecommerce.Admin.ViewModels;

/// <summary>
/// View model for Buy X Get Y Promotion create/edit forms.
/// </summary>
public class BuyXGetYPromotionViewModel
{
    public int PromotionID { get; set; }

    public Guid PromotionGuid { get; set; } = Guid.NewGuid();

    [TextInputComponent(
        Label = "Code Name",
        ExplanationText = "Unique identifier (e.g., buy-2-get-1-free)",
        WatermarkText = "buy-2-get-1-free",
        Order = 1)]
    [RequiredValidationRule(ErrorMessage = "Code Name is required")]
    public string PromotionName { get; set; } = string.Empty;

    [TextInputComponent(
        Label = "Display Name",
        ExplanationText = "Name shown to customers",
        WatermarkText = "Buy 2 Get 1 Free",
        Order = 2)]
    [RequiredValidationRule(ErrorMessage = "Display Name is required")]
    public string PromotionDisplayName { get; set; } = string.Empty;

    [TextAreaComponent(
        Label = "Description",
        ExplanationText = "Internal description of the promotion",
        Order = 3)]
    public string PromotionDescription { get; set; } = string.Empty;

    [NumberInputComponent(
        Label = "Buy Quantity",
        ExplanationText = "Number of items the customer must buy (the 'X' in Buy X Get Y)",
        Order = 4)]
    [RequiredValidationRule(ErrorMessage = "Buy Quantity is required")]
    public int BuyQuantity { get; set; } = 1;

    [NumberInputComponent(
        Label = "Get Quantity",
        ExplanationText = "Number of items the customer gets discounted (the 'Y' in Buy X Get Y)",
        Order = 5)]
    [RequiredValidationRule(ErrorMessage = "Get Quantity is required")]
    public int GetQuantity { get; set; } = 1;

    [DecimalNumberInputComponent(
        Label = "Get Discount Percentage",
        ExplanationText = "Percentage discount on the 'Get' items. 100 = free, 50 = half price.",
        Order = 6)]
    [RequiredValidationRule(ErrorMessage = "Discount Percentage is required")]
    public decimal GetDiscountPercentage { get; set; } = 100m;

    [TextAreaComponent(
        Label = "Target Categories",
        ExplanationText = "Comma-separated category codes. Leave empty to apply to all products.",
        Order = 7)]
    public string TargetCategories { get; set; } = string.Empty;

    [TextAreaComponent(
        Label = "Target Products",
        ExplanationText = "Comma-separated product content item IDs. Leave empty to apply to all products.",
        Order = 8)]
    public string TargetProducts { get; set; } = string.Empty;

    [DateTimeInputComponent(
        Label = "Active From",
        ExplanationText = "When the promotion becomes active",
        Order = 9)]
    [RequiredValidationRule(ErrorMessage = "Active From date is required")]
    public DateTime PromotionActiveFrom { get; set; } = DateTime.UtcNow;

    [DateTimeInputComponent(
        Label = "Active To",
        ExplanationText = "When the promotion expires (leave empty for no end date)",
        Order = 10)]
    public DateTime? PromotionActiveTo { get; set; }

    [CheckBoxComponent(
        Label = "Enabled",
        ExplanationText = "Uncheck to deactivate the promotion",
        Order = 11)]
    public bool PromotionEnabled { get; set; } = true;

    [NumberInputComponent(
        Label = "Order",
        ExplanationText = "Display order in listings",
        Order = 20)]
    public int PromotionOrder { get; set; } = 0;
}
