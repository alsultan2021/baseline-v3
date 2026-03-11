namespace Baseline.Ecommerce;

// TODO P3-3: Integrate a real shipping-rate provider (EasyPost, Shippo, carrier
// APIs) behind IShippingCostCalculator so rates are fetched at checkout rather
// than computed from static zone/weight tables. The current implementation is a
// good fallback; wrap it in a composite pattern alongside the live provider.

/// <summary>
/// Service for calculating shipping costs based on various criteria.
/// Supports zone-based, weight-based, and value-based shipping calculations.
/// </summary>
public interface IShippingCostCalculator
{
    /// <summary>
    /// Calculates shipping cost for a given request.
    /// </summary>
    /// <param name="request">The shipping calculation request containing items and destination.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The calculated shipping cost result.</returns>
    Task<ShippingCostResult> CalculateAsync(ShippingCostRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available shipping rates for a destination.
    /// </summary>
    /// <param name="request">The shipping rate request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Available shipping rates with costs.</returns>
    Task<IEnumerable<ShippingRate>> GetAvailableRatesAsync(ShippingRateRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates if shipping is available to a destination.
    /// </summary>
    /// <param name="address">The destination address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if shipping is available to the address.</returns>
    Task<bool> IsShippingAvailableAsync(Address address, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the shipping zone for an address.
    /// </summary>
    /// <param name="address">The destination address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The shipping zone, or null if not found.</returns>
    Task<ShippingZone?> GetShippingZoneAsync(Address address, CancellationToken cancellationToken = default);
}

#region Shipping Models

/// <summary>
/// Request for calculating shipping cost.
/// </summary>
public record ShippingCostRequest
{
    /// <summary>
    /// The destination address for shipping.
    /// </summary>
    public required Address DestinationAddress { get; init; }

    /// <summary>
    /// The origin address for shipping (optional, uses default if not provided).
    /// </summary>
    public Address? OriginAddress { get; init; }

    /// <summary>
    /// The items to be shipped.
    /// </summary>
    public IReadOnlyList<ShippingItem> Items { get; init; } = [];

    /// <summary>
    /// The selected shipping method ID (optional).
    /// </summary>
    public Guid? ShippingMethodId { get; init; }

    /// <summary>
    /// The cart subtotal for value-based shipping calculations.
    /// </summary>
    public Money? Subtotal { get; init; }
}

/// <summary>
/// An item included in shipping calculation.
/// </summary>
public record ShippingItem
{
    /// <summary>
    /// The product ID.
    /// </summary>
    public int ProductId { get; init; }

    /// <summary>
    /// The product SKU.
    /// </summary>
    public required string Sku { get; init; }

    /// <summary>
    /// The product name.
    /// </summary>
    public string Name { get; init; } = "";

    /// <summary>
    /// The quantity of items.
    /// </summary>
    public int Quantity { get; init; } = 1;

    /// <summary>
    /// The weight per unit (in configured unit).
    /// </summary>
    public decimal Weight { get; init; }

    /// <summary>
    /// The item dimensions (length x width x height).
    /// </summary>
    public Dimensions? Dimensions { get; init; }

    /// <summary>
    /// Whether this item requires special handling.
    /// </summary>
    public bool RequiresSpecialHandling { get; init; }

    /// <summary>
    /// Whether this item is fragile.
    /// </summary>
    public bool IsFragile { get; init; }

    /// <summary>
    /// Whether this item qualifies for free shipping.
    /// </summary>
    public bool FreeShipping { get; init; }
}

/// <summary>
/// Product dimensions for shipping calculations.
/// </summary>
public record Dimensions(decimal Length, decimal Width, decimal Height)
{
    /// <summary>
    /// Calculates the volume.
    /// </summary>
    public decimal Volume => Length * Width * Height;

    /// <summary>
    /// Gets the dimensional weight using standard divisor (139 for inches, 5000 for cm).
    /// </summary>
    public decimal GetDimensionalWeight(decimal divisor = 139) => Volume / divisor;
}

/// <summary>
/// Result of shipping cost calculation.
/// </summary>
public record ShippingCostResult
{
    /// <summary>
    /// Whether the calculation was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// The calculated shipping cost.
    /// </summary>
    public Money Cost { get; init; } = new();

    /// <summary>
    /// The shipping zone used for calculation.
    /// </summary>
    public ShippingZone? Zone { get; init; }

    /// <summary>
    /// The shipping method used.
    /// </summary>
    public ShippingMethod? Method { get; init; }

    /// <summary>
    /// Estimated delivery date range.
    /// </summary>
    public DeliveryEstimate? DeliveryEstimate { get; init; }

    /// <summary>
    /// Error message if calculation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Breakdown of cost components.
    /// </summary>
    public IReadOnlyList<ShippingCostComponent> CostBreakdown { get; init; } = [];

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static ShippingCostResult Successful(Money cost, ShippingZone? zone = null, ShippingMethod? method = null) =>
        new() { Success = true, Cost = cost, Zone = zone, Method = method };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static ShippingCostResult Failed(string errorMessage) =>
        new() { Success = false, ErrorMessage = errorMessage };
}

/// <summary>
/// Component of shipping cost breakdown.
/// </summary>
public record ShippingCostComponent(string Name, Money Amount, string? Description = null);

/// <summary>
/// Estimated delivery date range.
/// </summary>
public record DeliveryEstimate(DateTime? MinDate, DateTime? MaxDate, int? BusinessDays = null);

/// <summary>
/// Request for available shipping rates.
/// </summary>
public record ShippingRateRequest
{
    /// <summary>
    /// The destination address.
    /// </summary>
    public required Address DestinationAddress { get; init; }

    /// <summary>
    /// Total weight of shipment.
    /// </summary>
    public decimal TotalWeight { get; init; }

    /// <summary>
    /// Cart subtotal for value-based rates.
    /// </summary>
    public Money? Subtotal { get; init; }

    /// <summary>
    /// Number of items in shipment.
    /// </summary>
    public int ItemCount { get; init; }
}

/// <summary>
/// A shipping rate option.
/// </summary>
public record ShippingRate
{
    /// <summary>
    /// The shipping method.
    /// </summary>
    public required ShippingMethod Method { get; init; }

    /// <summary>
    /// The calculated cost for this rate.
    /// </summary>
    public required Money Cost { get; init; }

    /// <summary>
    /// Estimated delivery.
    /// </summary>
    public DeliveryEstimate? DeliveryEstimate { get; init; }

    /// <summary>
    /// Whether this is the cheapest option.
    /// </summary>
    public bool IsCheapest { get; init; }

    /// <summary>
    /// Whether this is the fastest option.
    /// </summary>
    public bool IsFastest { get; init; }
}

/// <summary>
/// A shipping zone for rate calculation.
/// </summary>
public class ShippingZone
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Zone code (e.g., "US-DOMESTIC", "CA", "INTERNATIONAL").
    /// </summary>
    public string Code { get; set; } = "";

    /// <summary>
    /// Display name.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Countries included in this zone.
    /// </summary>
    public IReadOnlyList<string> Countries { get; set; } = [];

    /// <summary>
    /// States/provinces included (if country-specific).
    /// </summary>
    public IReadOnlyList<string> States { get; set; } = [];

    /// <summary>
    /// Postal code patterns (regex or prefix).
    /// </summary>
    public IReadOnlyList<string> PostalCodePatterns { get; set; } = [];

    /// <summary>
    /// Base shipping rate for this zone.
    /// </summary>
    public decimal BaseRate { get; set; }

    /// <summary>
    /// Additional rate per weight unit.
    /// </summary>
    public decimal RatePerWeightUnit { get; set; }

    /// <summary>
    /// Additional rate per item.
    /// </summary>
    public decimal RatePerItem { get; set; }

    /// <summary>
    /// Minimum order value for free shipping.
    /// </summary>
    public decimal? FreeShippingThreshold { get; set; }

    /// <summary>
    /// Whether zone is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Display order.
    /// </summary>
    public int Order { get; set; }
}

#endregion
