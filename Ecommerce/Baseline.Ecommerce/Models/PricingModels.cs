namespace Baseline.Ecommerce;

/// <summary>
/// Calculation modes that determine which pricing steps execute.
/// Aligned with Kentico's PriceCalculationMode enum.
/// </summary>
public enum PriceCalculationMode
{
    /// <summary>
    /// Product listings, category pages, product detail pages.
    /// Executes: Product data loading, catalog promotions, line subtotals.
    /// </summary>
    Catalog,

    /// <summary>
    /// Shopping cart display before shipping selection.
    /// Executes: All Catalog steps plus order promotions, taxes, line totals, order totals.
    /// </summary>
    ShoppingCart,

    /// <summary>
    /// Final checkout with shipping.
    /// Executes: All steps including shipping calculation.
    /// </summary>
    Checkout
}

/// <summary>
/// Request for price calculation following Kentico's pipeline pattern.
/// </summary>
public record PriceCalculationRequest
{
    /// <summary>
    /// Items to calculate prices for.
    /// </summary>
    public ICollection<PriceCalculationRequestItem> Items { get; init; } = [];

    /// <summary>
    /// The calculation mode determining which steps to execute.
    /// </summary>
    public PriceCalculationMode Mode { get; init; } = PriceCalculationMode.Catalog;

    /// <summary>
    /// Language for localized pricing rules.
    /// </summary>
    public string? LanguageName { get; init; }

    /// <summary>
    /// Customer ID for customer-specific pricing.
    /// </summary>
    public int? CustomerId { get; init; }

    /// <summary>
    /// Shipping method ID (required for Checkout mode).
    /// </summary>
    public int? ShippingMethodId { get; init; }

    /// <summary>
    /// Payment method ID (optional).
    /// </summary>
    public int? PaymentMethodId { get; init; }

    /// <summary>
    /// Billing address for tax calculation.
    /// </summary>
    public Address? BillingAddress { get; init; }

    /// <summary>
    /// Shipping address for shipping and tax calculation.
    /// </summary>
    public Address? ShippingAddress { get; init; }

    /// <summary>
    /// Coupon codes to apply.
    /// </summary>
    public ICollection<string> CouponCodes { get; init; } = [];
}

/// <summary>
/// Item in a price calculation request.
/// </summary>
public record PriceCalculationRequestItem
{
    /// <summary>
    /// Product identifier (SKU or content item ID).
    /// </summary>
    public required string ProductIdentifier { get; init; }

    /// <summary>
    /// Quantity of items.
    /// </summary>
    public int Quantity { get; init; } = 1;

    /// <summary>
    /// Optional variant identifier.
    /// </summary>
    public string? VariantIdentifier { get; init; }

    /// <summary>
    /// Override unit price for virtual products (e.g., gift cards).
    /// When set, this price is used instead of looking up the product.
    /// </summary>
    public decimal? OverridePrice { get; init; }
}

/// <summary>
/// Result of price calculation following Kentico's pipeline pattern.
/// </summary>
public record PriceCalculationResult
{
    /// <summary>
    /// Itemized pricing results.
    /// </summary>
    public ICollection<PriceCalculationResultItem> Items { get; init; } = [];

    /// <summary>
    /// Total price of all items after catalog promotions (before order promotions).
    /// </summary>
    public decimal TotalPrice { get; init; }

    /// <summary>
    /// Total discount amount from catalog promotions.
    /// </summary>
    public decimal TotalCatalogDiscount { get; init; }

    /// <summary>
    /// Total discount amount from order promotions.
    /// </summary>
    public decimal TotalOrderDiscount { get; init; }

    /// <summary>
    /// Total tax amount.
    /// </summary>
    public decimal TotalTax { get; init; }

    /// <summary>
    /// Tax rate applied (e.g., 0.14975 for 14.975%).
    /// </summary>
    public decimal TaxRate { get; init; }

    /// <summary>
    /// Shipping price (only calculated in Checkout mode).
    /// </summary>
    public decimal ShippingPrice { get; init; }

    /// <summary>
    /// Grand total to be paid.
    /// </summary>
    public decimal GrandTotal { get; init; }

    /// <summary>
    /// Applied catalog promotions.
    /// </summary>
    public ICollection<AppliedPromotion> CatalogPromotions { get; init; } = [];

    /// <summary>
    /// Applied order promotions.
    /// </summary>
    public ICollection<AppliedPromotion> OrderPromotions { get; init; } = [];

    /// <summary>
    /// Calculation mode used.
    /// </summary>
    public PriceCalculationMode Mode { get; init; }

    /// <summary>
    /// Currency of all amounts.
    /// </summary>
    public string Currency { get; init; } = "USD";

    /// <summary>
    /// Individual tax entries for display (e.g., GST, QST as separate lines).
    /// </summary>
    public ICollection<TaxEntryInfo> TaxEntries { get; init; } = [];
}

/// <summary>
/// Individual tax entry for display (e.g., GST, QST as separate lines).
/// </summary>
public record TaxEntryInfo(string Name, decimal Rate, decimal Amount)
{
    public string FormattedRate => $"{Rate * 100:F3}";
    public string FormattedAmount => Amount.ToString("C");
    public string FormattedLabel => $"{Name} ({FormattedRate}%)";
}

/// <summary>
/// Individual item in the calculation result.
/// </summary>
public record PriceCalculationResultItem
{
    /// <summary>
    /// Product identifier from the request.
    /// </summary>
    public required string ProductIdentifier { get; init; }

    /// <summary>
    /// Quantity.
    /// </summary>
    public int Quantity { get; init; }

    /// <summary>
    /// Original unit price before any discounts.
    /// </summary>
    public decimal OriginalUnitPrice { get; init; }

    /// <summary>
    /// Unit price after catalog promotions.
    /// </summary>
    public decimal UnitPrice { get; init; }

    /// <summary>
    /// Line subtotal (UnitPrice * Quantity).
    /// </summary>
    public decimal LineSubtotal { get; init; }

    /// <summary>
    /// Tax amount for this line.
    /// </summary>
    public decimal LineTax { get; init; }

    /// <summary>
    /// Line total including tax.
    /// </summary>
    public decimal LineTotal { get; init; }

    /// <summary>
    /// Discount applied to this item.
    /// </summary>
    public decimal Discount { get; init; }

    /// <summary>
    /// Tax rate applied.
    /// </summary>
    public decimal TaxRate { get; init; }

    /// <summary>
    /// Applied catalog promotion for this item.
    /// </summary>
    public AppliedPromotion? CatalogPromotion { get; init; }
}

/// <summary>
/// Applied promotion information.
/// </summary>
public record AppliedPromotion
{
    /// <summary>
    /// Promotion identifier.
    /// </summary>
    public required string PromotionId { get; init; }

    /// <summary>
    /// Promotion name for display.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Discount amount.
    /// </summary>
    public decimal DiscountAmount { get; init; }

    /// <summary>
    /// Whether this is a catalog or order promotion.
    /// </summary>
    public bool IsCatalogPromotion { get; init; }
}
