namespace Baseline.Ecommerce;

/// <summary>
/// Represents tax information for display in UI.
/// </summary>
public class TaxDisplayInfo
{
    /// <summary>
    /// Display name for the tax (e.g., "Sales Tax", "VAT", "GST").
    /// </summary>
    public string Name { get; set; } = "Tax";

    /// <summary>
    /// Tax rate as a percentage (e.g., 8.25 for 8.25%).
    /// </summary>
    public decimal Rate { get; set; }

    /// <summary>
    /// Calculated tax amount.
    /// </summary>
    public Money Amount { get; set; } = Money.Zero();

    /// <summary>
    /// Tax category or type code.
    /// </summary>
    public string? CategoryCode { get; set; }

    /// <summary>
    /// Whether this tax is included in displayed prices.
    /// </summary>
    public bool IsIncludedInPrice { get; set; }

    /// <summary>
    /// Jurisdiction or region where this tax applies.
    /// </summary>
    public string? Jurisdiction { get; set; }

    /// <summary>
    /// Gets the rate formatted as a percentage string.
    /// </summary>
    public string FormattedRate => $"{Rate:0.##}%";
}

/// <summary>
/// Complete tax breakdown for an order or cart.
/// </summary>
public class TaxBreakdown
{
    /// <summary>
    /// Individual tax line items.
    /// </summary>
    public IList<TaxLineItem> LineItems { get; set; } = [];

    /// <summary>
    /// Total tax amount.
    /// </summary>
    public Money TotalTax { get; set; } = Money.Zero();

    /// <summary>
    /// Whether prices include tax (tax-inclusive pricing).
    /// </summary>
    public bool PricesIncludeTax { get; set; }

    /// <summary>
    /// The taxable subtotal before tax.
    /// </summary>
    public Money TaxableAmount { get; set; } = Money.Zero();

    /// <summary>
    /// Exempt amount (not subject to tax).
    /// </summary>
    public Money ExemptAmount { get; set; } = Money.Zero();

    /// <summary>
    /// Tax calculation timestamp.
    /// </summary>
    public DateTimeOffset CalculatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets whether there are multiple tax jurisdictions.
    /// </summary>
    public bool HasMultipleTaxes => LineItems.Count > 1;

    /// <summary>
    /// Gets a simple tax summary when there's only one tax.
    /// </summary>
    public TaxLineItem? PrimaryTax => LineItems.FirstOrDefault();
}

/// <summary>
/// Individual tax line item (for multi-jurisdiction taxes).
/// </summary>
public class TaxLineItem
{
    /// <summary>
    /// Tax jurisdiction name (e.g., "State", "County", "City").
    /// </summary>
    public string Jurisdiction { get; set; } = string.Empty;

    /// <summary>
    /// Tax type or name.
    /// </summary>
    public string TaxName { get; set; } = "Tax";

    /// <summary>
    /// Tax rate as percentage.
    /// </summary>
    public decimal Rate { get; set; }

    /// <summary>
    /// Tax amount for this jurisdiction.
    /// </summary>
    public Money Amount { get; set; } = Money.Zero();

    /// <summary>
    /// Tax authority ID (for reporting).
    /// </summary>
    public string? TaxAuthorityId { get; set; }

    /// <summary>
    /// Gets the display label combining jurisdiction and tax name.
    /// </summary>
    public string DisplayLabel => string.IsNullOrEmpty(Jurisdiction)
        ? TaxName
        : $"{Jurisdiction} {TaxName}";

    /// <summary>
    /// Gets the rate formatted as a percentage string.
    /// </summary>
    public string FormattedRate => $"{Rate:0.##}%";
}

/// <summary>
/// Tax-exempt status information.
/// </summary>
public class TaxExemptionInfo
{
    /// <summary>
    /// Whether the customer is tax-exempt.
    /// </summary>
    public bool IsExempt { get; set; }

    /// <summary>
    /// Tax exemption certificate number.
    /// </summary>
    public string? CertificateNumber { get; set; }

    /// <summary>
    /// Exemption reason code.
    /// </summary>
    public string? ReasonCode { get; set; }

    /// <summary>
    /// Exemption reason description.
    /// </summary>
    public string? ReasonDescription { get; set; }

    /// <summary>
    /// Jurisdictions where exemption applies.
    /// </summary>
    public IList<string> ApplicableJurisdictions { get; set; } = [];

    /// <summary>
    /// Exemption expiration date.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    /// Gets whether the exemption is still valid.
    /// </summary>
    public bool IsValid => IsExempt && (!ExpiresAt.HasValue || ExpiresAt > DateTimeOffset.UtcNow);
}

/// <summary>
/// View model for displaying tax summary in UI.
/// </summary>
public class TaxSummaryViewModel
{
    /// <summary>
    /// Tax breakdown with all line items.
    /// </summary>
    public TaxBreakdown Breakdown { get; set; } = new();

    /// <summary>
    /// Whether to show detailed breakdown or just total.
    /// </summary>
    public bool ShowDetails { get; set; }

    /// <summary>
    /// Currency code for formatting.
    /// </summary>
    public string CurrencyCode { get; set; } = "USD";

    /// <summary>
    /// Culture for number formatting.
    /// </summary>
    public string? CultureCode { get; set; }

    /// <summary>
    /// Label to display (e.g., "Sales Tax", "VAT").
    /// </summary>
    public string TaxLabel { get; set; } = "Tax";

    /// <summary>
    /// Whether to show "Tax included" message for inclusive pricing.
    /// </summary>
    public bool ShowInclusiveMessage { get; set; } = true;

    /// <summary>
    /// Custom message when tax is included in prices.
    /// </summary>
    public string InclusiveMessage { get; set; } = "Tax included";

    /// <summary>
    /// CSS class for the container.
    /// </summary>
    public string? CssClass { get; set; }
}

/// <summary>
/// Tax estimation request for UI.
/// </summary>
public record TaxEstimateRequest
{
    /// <summary>
    /// Country code (ISO 2-letter).
    /// </summary>
    public required string CountryCode { get; init; }

    /// <summary>
    /// State/province code.
    /// </summary>
    public string? StateCode { get; init; }

    /// <summary>
    /// Postal/ZIP code.
    /// </summary>
    public string? PostalCode { get; init; }

    /// <summary>
    /// City name.
    /// </summary>
    public string? City { get; init; }

    /// <summary>
    /// Subtotal amount to calculate tax on.
    /// </summary>
    public decimal Subtotal { get; init; }

    /// <summary>
    /// Product category for category-specific rates.
    /// </summary>
    public string? ProductCategory { get; init; }
}

/// <summary>
/// Tax estimation result.
/// </summary>
public record TaxEstimateResult
{
    /// <summary>
    /// Whether estimation was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Error message if estimation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Estimated tax breakdown.
    /// </summary>
    public TaxBreakdown? Breakdown { get; init; }

    /// <summary>
    /// Whether this is an estimate (vs. final calculation).
    /// </summary>
    public bool IsEstimate { get; init; } = true;

    /// <summary>
    /// Disclaimer message for estimates.
    /// </summary>
    public string? Disclaimer { get; init; }

    public static TaxEstimateResult Succeeded(TaxBreakdown breakdown, string? disclaimer = null) =>
        new()
        {
            Success = true,
            Breakdown = breakdown,
            IsEstimate = true,
            Disclaimer = disclaimer ?? "Tax amount is an estimate and may vary."
        };

    public static TaxEstimateResult Failed(string message) =>
        new() { Success = false, ErrorMessage = message };
}

/// <summary>
/// Price display options for tax.
/// </summary>
public class TaxDisplayOptions
{
    /// <summary>
    /// Whether to show prices with tax included.
    /// </summary>
    public bool ShowPricesWithTax { get; set; }

    /// <summary>
    /// Whether to show both with/without tax prices.
    /// </summary>
    public bool ShowBothPrices { get; set; }

    /// <summary>
    /// Label for price excluding tax.
    /// </summary>
    public string ExcludingTaxLabel { get; set; } = "excl. tax";

    /// <summary>
    /// Label for price including tax.
    /// </summary>
    public string IncludingTaxLabel { get; set; } = "incl. tax";

    /// <summary>
    /// Tax name to display (e.g., "VAT", "GST", "Sales Tax").
    /// </summary>
    public string TaxName { get; set; } = "Tax";

    /// <summary>
    /// Number of decimal places for tax amounts.
    /// </summary>
    public int DecimalPlaces { get; set; } = 2;

    /// <summary>
    /// Whether to show tax rate percentage.
    /// </summary>
    public bool ShowTaxRate { get; set; }
}
#region External Tax Service Models

/// <summary>
/// Request for external tax calculation.
/// </summary>
public class TaxCalculationRequest
{
    /// <summary>
    /// Order or transaction ID for reference.
    /// </summary>
    public string? TransactionId { get; init; }

    /// <summary>
    /// Customer ID for customer-specific exemptions.
    /// </summary>
    public int? CustomerId { get; init; }

    /// <summary>
    /// Customer tax exemption certificate number.
    /// </summary>
    public string? ExemptionCertificateNumber { get; init; }

    /// <summary>
    /// Shipping origin address.
    /// </summary>
    public Address? OriginAddress { get; init; }

    /// <summary>
    /// Shipping destination address.
    /// </summary>
    public required Address DestinationAddress { get; init; }

    /// <summary>
    /// Line items to calculate tax for.
    /// </summary>
    public required IReadOnlyList<TaxCalculationLineItem> LineItems { get; init; }

    /// <summary>
    /// Shipping amount (if taxable).
    /// </summary>
    public Money? ShippingAmount { get; init; }

    /// <summary>
    /// Discount amount applied to order.
    /// </summary>
    public Money? DiscountAmount { get; init; }

    /// <summary>
    /// Currency code for amounts.
    /// </summary>
    public string CurrencyCode { get; init; } = "USD";

    /// <summary>
    /// Whether this is a tax estimate (preview) or final calculation (commit).
    /// </summary>
    public bool IsEstimate { get; init; } = true;

    /// <summary>
    /// Transaction date for tax rate determination.
    /// </summary>
    public DateTimeOffset TransactionDate { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Line item for tax calculation.
/// </summary>
public class TaxCalculationLineItem
{
    /// <summary>
    /// Line item identifier.
    /// </summary>
    public required string LineId { get; init; }

    /// <summary>
    /// Product SKU.
    /// </summary>
    public string? Sku { get; init; }

    /// <summary>
    /// Product description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Quantity of items.
    /// </summary>
    public decimal Quantity { get; init; } = 1;

    /// <summary>
    /// Unit price (before tax).
    /// </summary>
    public required Money UnitPrice { get; init; }

    /// <summary>
    /// Line total (before tax).
    /// </summary>
    public Money LineTotal => new() { Amount = UnitPrice.Amount * Quantity, Currency = UnitPrice.Currency };

    /// <summary>
    /// Tax code for this item (e.g., Avalara tax code).
    /// </summary>
    public string? TaxCode { get; init; }

    /// <summary>
    /// Whether this item is tax-exempt.
    /// </summary>
    public bool IsExempt { get; init; }

    /// <summary>
    /// Item category for tax determination.
    /// </summary>
    public string? ItemCategory { get; init; }
}

/// <summary>
/// Result of external tax calculation.
/// </summary>
public class TaxCalculationResult
{
    /// <summary>
    /// Whether the calculation was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Error message if calculation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Provider-specific transaction ID.
    /// </summary>
    public string? ProviderTransactionId { get; init; }

    /// <summary>
    /// Total tax amount.
    /// </summary>
    public Money TotalTax { get; init; } = Money.Zero();

    /// <summary>
    /// Tax breakdown by line item.
    /// </summary>
    public IReadOnlyList<TaxCalculationLineResult> LineResults { get; init; } = [];

    /// <summary>
    /// Tax breakdown by jurisdiction.
    /// </summary>
    public IReadOnlyList<TaxJurisdictionResult> JurisdictionBreakdown { get; init; } = [];

    /// <summary>
    /// Taxable amount (total before tax).
    /// </summary>
    public Money TaxableAmount { get; init; } = Money.Zero();

    /// <summary>
    /// Exempt amount (not subject to tax).
    /// </summary>
    public Money ExemptAmount { get; init; } = Money.Zero();

    /// <summary>
    /// Tax on shipping.
    /// </summary>
    public Money ShippingTax { get; init; } = Money.Zero();

    /// <summary>
    /// Effective combined tax rate.
    /// </summary>
    public decimal EffectiveRate { get; init; }

    /// <summary>
    /// When the calculation was performed.
    /// </summary>
    public DateTimeOffset CalculatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static TaxCalculationResult Succeeded(
        Money totalTax,
        IReadOnlyList<TaxCalculationLineResult> lineResults,
        IReadOnlyList<TaxJurisdictionResult> jurisdictionBreakdown,
        string? providerTransactionId = null) =>
        new()
        {
            Success = true,
            TotalTax = totalTax,
            LineResults = lineResults,
            JurisdictionBreakdown = jurisdictionBreakdown,
            ProviderTransactionId = providerTransactionId
        };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static TaxCalculationResult Failed(string message) =>
        new() { Success = false, ErrorMessage = message };
}

/// <summary>
/// Tax calculation result for a single line item.
/// </summary>
public class TaxCalculationLineResult
{
    /// <summary>
    /// Line item identifier.
    /// </summary>
    public required string LineId { get; init; }

    /// <summary>
    /// Taxable amount for this line.
    /// </summary>
    public Money TaxableAmount { get; init; } = Money.Zero();

    /// <summary>
    /// Tax amount for this line.
    /// </summary>
    public Money TaxAmount { get; init; } = Money.Zero();

    /// <summary>
    /// Exempt amount for this line.
    /// </summary>
    public Money ExemptAmount { get; init; } = Money.Zero();

    /// <summary>
    /// Effective tax rate for this line.
    /// </summary>
    public decimal EffectiveRate { get; init; }

    /// <summary>
    /// Whether this line was tax-exempt.
    /// </summary>
    public bool IsExempt { get; init; }

    /// <summary>
    /// Tax details by jurisdiction for this line.
    /// </summary>
    public IReadOnlyList<TaxJurisdictionResult> JurisdictionDetails { get; init; } = [];
}

/// <summary>
/// Tax breakdown by jurisdiction.
/// </summary>
public class TaxJurisdictionResult
{
    /// <summary>
    /// Jurisdiction type (e.g., "State", "County", "City", "District").
    /// </summary>
    public string JurisdictionType { get; init; } = string.Empty;

    /// <summary>
    /// Jurisdiction name (e.g., "Texas", "Harris County").
    /// </summary>
    public string JurisdictionName { get; init; } = string.Empty;

    /// <summary>
    /// Tax rate for this jurisdiction.
    /// </summary>
    public decimal Rate { get; init; }

    /// <summary>
    /// Tax amount for this jurisdiction.
    /// </summary>
    public Money TaxAmount { get; init; } = Money.Zero();

    /// <summary>
    /// Taxable amount in this jurisdiction.
    /// </summary>
    public Money TaxableAmount { get; init; } = Money.Zero();

    /// <summary>
    /// Tax authority code.
    /// </summary>
    public string? TaxAuthorityCode { get; init; }
}

/// <summary>
/// Result of address validation.
/// </summary>
public class AddressValidationResult
{
    /// <summary>
    /// Whether the address is valid.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Error or warning messages.
    /// </summary>
    public IReadOnlyList<string> Messages { get; init; } = [];

    /// <summary>
    /// Suggested corrected address (if different from input).
    /// </summary>
    public Address? SuggestedAddress { get; init; }

    /// <summary>
    /// Whether the address was modified/corrected.
    /// </summary>
    public bool WasCorrected { get; init; }

    /// <summary>
    /// Address precision/quality indicator.
    /// </summary>
    public AddressPrecision Precision { get; init; }

    /// <summary>
    /// Creates a valid result.
    /// </summary>
    public static AddressValidationResult Valid(Address? suggestedAddress = null) =>
        new()
        {
            IsValid = true,
            SuggestedAddress = suggestedAddress,
            WasCorrected = suggestedAddress != null,
            Precision = AddressPrecision.StreetLevel
        };

    /// <summary>
    /// Creates an invalid result.
    /// </summary>
    public static AddressValidationResult Invalid(params string[] messages) =>
        new() { IsValid = false, Messages = messages };
}

/// <summary>
/// Address precision levels.
/// </summary>
public enum AddressPrecision
{
    /// <summary>
    /// Unknown or invalid precision.
    /// </summary>
    Unknown,

    /// <summary>
    /// Country-level only.
    /// </summary>
    CountryLevel,

    /// <summary>
    /// State/province level.
    /// </summary>
    StateLevel,

    /// <summary>
    /// City level.
    /// </summary>
    CityLevel,

    /// <summary>
    /// ZIP/postal code level.
    /// </summary>
    PostalCodeLevel,

    /// <summary>
    /// Street level (most precise).
    /// </summary>
    StreetLevel,

    /// <summary>
    /// Rooftop/point level (exact location).
    /// </summary>
    PointLevel
}

#endregion