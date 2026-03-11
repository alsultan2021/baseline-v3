using Kentico.Xperience.Admin.Base.FormAnnotations;
using Baseline.Ecommerce.Admin.DataProviders;

namespace Baseline.Ecommerce.Admin.ViewModels;

/// <summary>
/// View model for Gift Card create/edit forms.
/// </summary>
public class GiftCardViewModel
{
    /// <summary>
    /// Gift card ID (hidden on create).
    /// </summary>
    public int GiftCardID { get; set; }

    /// <summary>
    /// Gift card GUID.
    /// </summary>
    public Guid GiftCardGuid { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gift card redemption code.
    /// </summary>
    [TextInputComponent(
        Label = "Gift Card Code",
        ExplanationText = "Unique code the customer will use to redeem the gift card (auto-generated if left empty)",
        WatermarkText = "GIFT-XXXX-XXXX",
        Order = 1)]
    public string GiftCardCode { get; set; } = string.Empty;

    /// <summary>
    /// Initial amount loaded on the gift card.
    /// </summary>
    [DecimalNumberInputComponent(
        Label = "Initial Amount",
        ExplanationText = "The total value to load onto the gift card",
        Order = 2)]
    [RequiredValidationRule(ErrorMessage = "Initial Amount is required")]
    public decimal GiftCardInitialAmount { get; set; } = 0m;

    /// <summary>
    /// Remaining balance on the gift card (managed automatically, not shown in create form).
    /// </summary>
    public decimal GiftCardRemainingBalance { get; set; } = 0m;

    /// <summary>
    /// Currency ID for the gift card.
    /// </summary>
    [DropDownComponent(
        Label = "Currency",
        ExplanationText = "Currency for the gift card value",
        DataProviderType = typeof(CurrencyDataProvider),
        Order = 4)]
    [RequiredValidationRule(ErrorMessage = "Currency is required")]
    public string GiftCardCurrencyID { get; set; } = string.Empty;

    /// <summary>
    /// Gift card status.
    /// </summary>
    [DropDownComponent(
        Label = "Status",
        ExplanationText = "Current status of the gift card",
        DataProviderType = typeof(GiftCardStatusDataProvider),
        Order = 5)]
    public string GiftCardStatus { get; set; } = "Active";

    /// <summary>
    /// Expiration date (optional).
    /// </summary>
    [DateTimeInputComponent(
        Label = "Expires At",
        ExplanationText = "Optional expiration date (leave empty for no expiration)",
        Order = 6)]
    public DateTime? GiftCardExpiresAt { get; set; }

    /// <summary>
    /// Whether the gift card is enabled.
    /// </summary>
    [CheckBoxComponent(
        Label = "Enabled",
        ExplanationText = "Uncheck to disable the gift card",
        Order = 7)]
    public bool GiftCardEnabled { get; set; } = true;

    /// <summary>
    /// Admin notes.
    /// </summary>
    [TextAreaComponent(
        Label = "Admin Notes",
        ExplanationText = "Internal notes about this gift card (not visible to customers)",
        Order = 10)]
    public string? GiftCardNotes { get; set; }

    // Hidden properties for data persistence (no form annotation = not shown in UI)
    public int? GiftCardRecipientMemberID { get; set; }
}
