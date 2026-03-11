using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Baseline.Ecommerce.Admin.ViewModels;

/// <summary>
/// View model for Currency create/edit forms.
/// </summary>
public class CurrencyViewModel
{
    /// <summary>
    /// Currency ID (primary key).
    /// </summary>
    public int CurrencyID { get; set; }

    /// <summary>
    /// Currency GUID.
    /// </summary>
    public Guid CurrencyGuid { get; set; } = Guid.NewGuid();

    /// <summary>
    /// ISO 4217 currency code (e.g., USD, EUR, GBP).
    /// </summary>
    [TextInputComponent(
        Label = "Currency Code",
        ExplanationText = "ISO 4217 3-letter currency code (e.g., USD, EUR, GBP)",
        WatermarkText = "USD",
        Order = 1)]
    [RequiredValidationRule(ErrorMessage = "Currency Code is required")]
    [MaxLengthValidationRule(3, ErrorMessage = "Currency code must be exactly 3 characters")]
    [MinLengthValidationRule(3, ErrorMessage = "Currency code must be exactly 3 characters")]
    public string CurrencyCode { get; set; } = string.Empty;

    /// <summary>
    /// Currency display name.
    /// </summary>
    [TextInputComponent(
        Label = "Display Name",
        ExplanationText = "Full name of the currency",
        WatermarkText = "US Dollar",
        Order = 2)]
    [RequiredValidationRule(ErrorMessage = "Display Name is required")]
    public string CurrencyDisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Currency symbol (e.g., $, €, £).
    /// </summary>
    [TextInputComponent(
        Label = "Symbol",
        ExplanationText = "Currency symbol for display (e.g., $, €, £)",
        WatermarkText = "$",
        Order = 3)]
    public string CurrencySymbol { get; set; } = string.Empty;

    /// <summary>
    /// Number of decimal places for this currency.
    /// </summary>
    [NumberInputComponent(
        Label = "Decimal Places",
        ExplanationText = "Number of decimal places (e.g., 2 for USD, 0 for JPY)",
        Order = 4)]
    public int CurrencyDecimalPlaces { get; set; } = 2;

    /// <summary>
    /// Format pattern for displaying amounts.
    /// </summary>
    [TextInputComponent(
        Label = "Format Pattern",
        ExplanationText = "Pattern for formatting amounts (e.g., {symbol}{amount} or {amount} {code})",
        WatermarkText = "{symbol}{amount}",
        Order = 5)]
    public string CurrencyFormatPattern { get; set; } = "{symbol}{amount}";

    /// <summary>
    /// Whether the currency is enabled.
    /// </summary>
    [CheckBoxComponent(
        Label = "Enabled",
        ExplanationText = "If unchecked, this currency will not be available for selection",
        Order = 6)]
    public bool CurrencyEnabled { get; set; } = true;

    /// <summary>
    /// Display order.
    /// </summary>
    [NumberInputComponent(
        Label = "Display Order",
        ExplanationText = "Order in which the currency appears in lists",
        Order = 7)]
    public int CurrencyOrder { get; set; }
}
