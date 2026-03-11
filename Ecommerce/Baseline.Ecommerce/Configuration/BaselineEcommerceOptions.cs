namespace Baseline.Ecommerce;

/// <summary>
/// Configuration options for Baseline v3 Ecommerce module.
/// </summary>
public class BaselineEcommerceOptions
{
    /// <summary>
    /// Enable shopping cart functionality.
    /// Default: true
    /// </summary>
    public bool EnableCart { get; set; } = true;

    /// <summary>
    /// Enable checkout workflow.
    /// Default: true
    /// </summary>
    public bool EnableCheckout { get; set; } = true;

    /// <summary>
    /// Enable guest checkout (without account).
    /// Default: true
    /// </summary>
    public bool EnableGuestCheckout { get; set; } = true;

    /// <summary>
    /// Enable order history for logged-in users.
    /// Default: true
    /// </summary>
    public bool EnableOrderHistory { get; set; } = true;

    /// <summary>
    /// Enable wishlist functionality.
    /// Default: true
    /// </summary>
    public bool EnableWishlist { get; set; } = true;

    /// <summary>
    /// Enable product comparison.
    /// Default: false
    /// </summary>
    public bool EnableProductComparison { get; set; } = false;

    /// <summary>
    /// Enable the Baseline Automation Engine for ecommerce triggers and workflows.
    /// When enabled, registers automation services, trigger handlers, and background processors.
    /// Default: false (must be explicitly enabled).
    /// </summary>
    public bool EnableAutomation { get; set; }

    /// <summary>
    /// Cart configuration options.
    /// </summary>
    public CartOptions Cart { get; set; } = new();

    /// <summary>
    /// Checkout configuration options.
    /// </summary>
    public CheckoutOptions Checkout { get; set; } = new();

    /// <summary>
    /// Pricing configuration options.
    /// </summary>
    public PricingOptions Pricing { get; set; } = new();

    /// <summary>
    /// Shipping configuration options.
    /// </summary>
    public ShippingOptions Shipping { get; set; } = new();

    /// <summary>
    /// Inventory configuration options.
    /// </summary>
    public InventoryOptions Inventory { get; set; } = new();
}

/// <summary>
/// Cart configuration options.
/// </summary>
public class CartOptions
{
    /// <summary>
    /// Cart session timeout in minutes.
    /// Default: 1440 (24 hours)
    /// </summary>
    public int SessionTimeoutMinutes { get; set; } = 1440;

    /// <summary>
    /// Maximum items allowed in cart.
    /// Default: 100
    /// </summary>
    public int MaxItems { get; set; } = 100;

    /// <summary>
    /// Maximum quantity per item.
    /// Default: 99
    /// </summary>
    public int MaxQuantityPerItem { get; set; } = 99;

    /// <summary>
    /// Persist cart for logged-in users.
    /// Default: true
    /// </summary>
    public bool PersistForLoggedInUsers { get; set; } = true;

    /// <summary>
    /// Merge anonymous cart with user cart on login.
    /// Default: true
    /// </summary>
    public bool MergeOnLogin { get; set; } = true;
}

/// <summary>
/// Checkout configuration options.
/// </summary>
public class CheckoutOptions
{
    /// <summary>
    /// Checkout flow steps.
    /// </summary>
    public List<string> Steps { get; set; } = ["cart", "shipping", "payment", "review", "confirmation"];

    /// <summary>
    /// Enable address validation.
    /// Default: false
    /// </summary>
    public bool EnableAddressValidation { get; set; } = false;

    /// <summary>
    /// Require terms acceptance.
    /// Default: true
    /// </summary>
    public bool RequireTermsAcceptance { get; set; } = true;

    /// <summary>
    /// Enable order notes.
    /// Default: true
    /// </summary>
    public bool EnableOrderNotes { get; set; } = true;

    /// <summary>
    /// Confirmation page URL.
    /// </summary>
    public string ConfirmationUrl { get; set; } = "/checkout/confirmation";
}

/// <summary>
/// Pricing configuration options.
/// </summary>
public class PricingOptions
{
    /// <summary>
    /// Default currency code.
    /// Default: "USD"
    /// </summary>
    public string DefaultCurrency { get; set; } = "USD";

    /// <summary>
    /// Enable tax calculation.
    /// Default: true
    /// </summary>
    public bool EnableTax { get; set; } = true;

    /// <summary>
    /// Default tax rate as a decimal (e.g., 0.07 for 7%).
    /// Used when tax calculation service is not configured.
    /// Default: 0 (no tax)
    /// </summary>
    public decimal DefaultTaxRate { get; set; } = 0m;

    /// <summary>
    /// Display prices including tax.
    /// Default: false
    /// </summary>
    public bool DisplayPricesWithTax { get; set; } = false;

    /// <summary>
    /// Number of decimal places for prices.
    /// Default: 2
    /// </summary>
    public int DecimalPlaces { get; set; } = 2;

    /// <summary>
    /// Enable discount codes.
    /// Default: true
    /// </summary>
    public bool EnableDiscountCodes { get; set; } = true;

    /// <summary>
    /// Tax rates by region code.
    /// Allows overriding the default tax rate for specific regions.
    /// Key is the region code (e.g., "QC", "ON"), value is the tax rate as a decimal.
    /// </summary>
    public Dictionary<string, decimal> TaxRatesByRegion { get; set; } = [];
}

/// <summary>
/// Shipping configuration options.
/// </summary>
public class ShippingOptions
{
    /// <summary>
    /// Default flat rate shipping cost.
    /// Default: 0
    /// </summary>
    public decimal DefaultFlatRate { get; set; } = 0m;

    /// <summary>
    /// Free shipping threshold amount.
    /// Orders above this amount get free shipping.
    /// Null means disabled.
    /// </summary>
    public decimal? FreeShippingThreshold { get; set; }

    /// <summary>
    /// Enable free shipping for orders above threshold.
    /// Default: false
    /// </summary>
    public bool EnableFreeShippingThreshold { get; set; } = false;

    /// <summary>
    /// Additional rate per weight unit.
    /// Used for weight-based shipping calculations.
    /// Default: 0
    /// </summary>
    public decimal RatePerWeightUnit { get; set; } = 0m;

    /// <summary>
    /// Additional rate per item.
    /// Used for item-based shipping calculations.
    /// Default: 0
    /// </summary>
    public decimal RatePerItem { get; set; } = 0m;
}

/// <summary>
/// Inventory configuration options.
/// </summary>
public class InventoryOptions
{
    /// <summary>
    /// Enable inventory tracking.
    /// Default: true
    /// </summary>
    public bool EnableInventoryTracking { get; set; } = true;

    /// <summary>
    /// Allow backorders.
    /// Default: false
    /// </summary>
    public bool AllowBackorders { get; set; } = false;

    /// <summary>
    /// Low stock threshold for notifications.
    /// Default: 10
    /// </summary>
    public int LowStockThreshold { get; set; } = 10;

    /// <summary>
    /// Reserve inventory on add to cart.
    /// Default: false
    /// </summary>
    public bool ReserveOnAddToCart { get; set; } = false;
}
