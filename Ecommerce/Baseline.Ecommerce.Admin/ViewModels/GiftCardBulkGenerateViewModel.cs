using Kentico.Xperience.Admin.Base.FormAnnotations;
using Baseline.Ecommerce.Admin.DataProviders;

namespace Baseline.Ecommerce.Admin.ViewModels;

/// <summary>
/// View model for bulk Gift Card generation.
/// </summary>
public class GiftCardBulkGenerateViewModel
{
    /// <summary>
    /// Number of gift cards to generate.
    /// </summary>
    [NumberInputComponent(
        Label = "Quantity",
        ExplanationText = "Number of gift cards to generate (1-100)",
        Order = 1)]
    [RequiredValidationRule(ErrorMessage = "Quantity is required")]
    public int Quantity { get; set; } = 10;

    /// <summary>
    /// Amount for each gift card.
    /// </summary>
    [DecimalNumberInputComponent(
        Label = "Amount Per Card",
        ExplanationText = "The value to load onto each gift card",
        Order = 2)]
    [RequiredValidationRule(ErrorMessage = "Amount is required")]
    public decimal Amount { get; set; } = 50m;

    /// <summary>
    /// Currency ID for the gift cards.
    /// </summary>
    [DropDownComponent(
        Label = "Currency",
        ExplanationText = "Currency for all generated gift cards",
        DataProviderType = typeof(CurrencyDataProvider),
        Order = 3)]
    [RequiredValidationRule(ErrorMessage = "Currency is required")]
    public string CurrencyID { get; set; } = string.Empty;

    /// <summary>
    /// Code prefix for generated cards.
    /// </summary>
    [TextInputComponent(
        Label = "Code Prefix",
        ExplanationText = "Optional prefix for generated codes (e.g., 'PROMO' creates PROMO-XXXX-XXXX)",
        WatermarkText = "GIFT",
        Order = 4)]
    public string CodePrefix { get; set; } = "GIFT";

    /// <summary>
    /// Expiration date (optional).
    /// </summary>
    [DateTimeInputComponent(
        Label = "Expires At",
        ExplanationText = "Optional expiration date for all generated cards",
        Order = 5)]
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Notes for the batch.
    /// </summary>
    [TextAreaComponent(
        Label = "Notes",
        ExplanationText = "Optional notes for all generated gift cards (e.g., campaign name)",
        Order = 6)]
    public string Notes { get; set; } = string.Empty;
}
