using Baseline.Ecommerce.Admin.DataProviders;
using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Baseline.Ecommerce.Admin.ViewModels;

/// <summary>
/// View model for Currency Exchange Rate create/edit forms.
/// </summary>
public class CurrencyExchangeRateViewModel
{
    /// <summary>
    /// Exchange rate ID (primary key).
    /// </summary>
    public int ExchangeRateID { get; set; }

    /// <summary>
    /// Exchange rate GUID.
    /// </summary>
    public Guid ExchangeRateGuid { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Source currency ID (from currency).
    /// </summary>
    [DropDownComponent(
        Label = "From Currency",
        ExplanationText = "The source currency to convert from",
        DataProviderType = typeof(CurrencyDataProvider),
        Order = 1)]
    [RequiredValidationRule(ErrorMessage = "From Currency is required")]
    public int ExchangeRateFromCurrencyID { get; set; }

    /// <summary>
    /// Target currency ID (to currency).
    /// </summary>
    [DropDownComponent(
        Label = "To Currency",
        ExplanationText = "The target currency to convert to",
        DataProviderType = typeof(CurrencyDataProvider),
        Order = 2)]
    [RequiredValidationRule(ErrorMessage = "To Currency is required")]
    public int ExchangeRateToCurrencyID { get; set; }

    /// <summary>
    /// Exchange rate value.
    /// </summary>
    [DecimalNumberInputComponent(
        Label = "Exchange Rate",
        ExplanationText = "The conversion rate (e.g., 0.85 to convert 1 USD to 0.85 EUR)",
        Order = 3)]
    [RequiredValidationRule(ErrorMessage = "Exchange Rate is required")]
    public decimal ExchangeRateValue { get; set; } = 1m;

    /// <summary>
    /// Valid from date.
    /// </summary>
    [DateTimeInputComponent(
        Label = "Valid From",
        ExplanationText = "Date and time when this rate becomes valid",
        Order = 4)]
    public DateTime? ExchangeRateValidFrom { get; set; }

    /// <summary>
    /// Valid to date.
    /// </summary>
    [DateTimeInputComponent(
        Label = "Valid To",
        ExplanationText = "Date and time when this rate expires (leave empty for no expiration)",
        Order = 5)]
    public DateTime? ExchangeRateValidTo { get; set; }

    /// <summary>
    /// Rate source (e.g., Manual, API, Bank).
    /// </summary>
    [TextInputComponent(
        Label = "Source",
        ExplanationText = "Source of this exchange rate (e.g., Manual, ECB, OpenExchangeRates)",
        WatermarkText = "Manual",
        Order = 6)]
    public string ExchangeRateSource { get; set; } = "Manual";

    /// <summary>
    /// Whether the exchange rate is enabled.
    /// </summary>
    [CheckBoxComponent(
        Label = "Enabled",
        ExplanationText = "If unchecked, this exchange rate will not be used for conversions",
        Order = 7)]
    public bool ExchangeRateEnabled { get; set; } = true;
}
