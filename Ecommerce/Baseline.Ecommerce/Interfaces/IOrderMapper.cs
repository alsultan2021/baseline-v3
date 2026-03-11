namespace Baseline.Ecommerce;

/// <summary>
/// Service for mapping and transforming order data between different representations.
/// Useful for complex order transformations, external system integrations, and custom order workflows.
/// </summary>
public interface IOrderMapper
{
    /// <summary>
    /// Maps a checkout session to an order creation request.
    /// </summary>
    /// <param name="session">The checkout session to map.</param>
    /// <param name="priceResult">The calculated prices for the order.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The order creation request.</returns>
    Task<CreateOrderRequest> MapToCreateRequestAsync(
        CheckoutSession session,
        PriceCalculationResult priceResult,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Maps an order to an external system format.
    /// </summary>
    /// <param name="order">The order to map.</param>
    /// <param name="context">Optional mapping context with additional data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The mapped order data as a dictionary.</returns>
    Task<OrderMappingResult> MapToExternalAsync(
        Order order,
        OrderMappingContext? context = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Maps external order data to an internal order format.
    /// </summary>
    /// <param name="externalData">The external order data.</param>
    /// <param name="context">Optional mapping context with additional data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The mapped order.</returns>
    Task<OrderMappingResult<Order>> MapFromExternalAsync(
        IDictionary<string, object> externalData,
        OrderMappingContext? context = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Maps an order to a summary view model.
    /// </summary>
    /// <param name="order">The order to map.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The order summary.</returns>
    Task<OrderSummary> MapToSummaryAsync(Order order, CancellationToken cancellationToken = default);

    /// <summary>
    /// Maps order items with product data enrichment.
    /// </summary>
    /// <param name="order">The order containing items to map.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The enriched order items.</returns>
    Task<IEnumerable<OrderItemDetail>> MapOrderItemsAsync(Order order, CancellationToken cancellationToken = default);

    /// <summary>
    /// Maps an address from order format to external format.
    /// </summary>
    /// <param name="address">The address to map.</param>
    /// <returns>The mapped address data.</returns>
    IDictionary<string, object> MapAddress(Address address);

    /// <summary>
    /// Maps external address data to internal format.
    /// </summary>
    /// <param name="externalAddress">The external address data.</param>
    /// <returns>The mapped address.</returns>
    Address MapFromExternalAddress(IDictionary<string, object> externalAddress);
}

#region Order Mapping Models

/// <summary>
/// Context for order mapping operations.
/// </summary>
public class OrderMappingContext
{
    /// <summary>
    /// The target system name (e.g., "ERP", "Fulfillment", "Accounting").
    /// </summary>
    public string? TargetSystem { get; set; }

    /// <summary>
    /// The mapping format version.
    /// </summary>
    public string? FormatVersion { get; set; }

    /// <summary>
    /// Additional metadata for mapping.
    /// </summary>
    public IDictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// Whether to include full item details.
    /// </summary>
    public bool IncludeItemDetails { get; set; } = true;

    /// <summary>
    /// Whether to include customer data.
    /// </summary>
    public bool IncludeCustomerData { get; set; } = true;

    /// <summary>
    /// Whether to include payment details.
    /// </summary>
    public bool IncludePaymentDetails { get; set; } = true;

    /// <summary>
    /// Field mappings for custom transformations.
    /// </summary>
    public IDictionary<string, string> FieldMappings { get; set; } = new Dictionary<string, string>();
}

/// <summary>
/// Result of an order mapping operation.
/// </summary>
public record OrderMappingResult
{
    /// <summary>
    /// Whether the mapping was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// The mapped data.
    /// </summary>
    public IDictionary<string, object> Data { get; init; } = new Dictionary<string, object>();

    /// <summary>
    /// Warnings generated during mapping.
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = [];

    /// <summary>
    /// Error message if mapping failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static OrderMappingResult Successful(IDictionary<string, object> data, IEnumerable<string>? warnings = null) =>
        new() { Success = true, Data = data, Warnings = warnings?.ToList() ?? [] };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static OrderMappingResult Failed(string errorMessage) =>
        new() { Success = false, ErrorMessage = errorMessage };
}

/// <summary>
/// Generic result of an order mapping operation.
/// </summary>
public record OrderMappingResult<T>
{
    /// <summary>
    /// Whether the mapping was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// The mapped result.
    /// </summary>
    public T? Result { get; init; }

    /// <summary>
    /// Warnings generated during mapping.
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = [];

    /// <summary>
    /// Error message if mapping failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static OrderMappingResult<T> Successful(T result, IEnumerable<string>? warnings = null) =>
        new() { Success = true, Result = result, Warnings = warnings?.ToList() ?? [] };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static OrderMappingResult<T> Failed(string errorMessage) =>
        new() { Success = false, ErrorMessage = errorMessage };
}

/// <summary>
/// Detailed order item with enriched product data.
/// </summary>
public class OrderItemDetail
{
    /// <summary>
    /// The order item ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Product SKU.
    /// </summary>
    public string Sku { get; set; } = "";

    /// <summary>
    /// Product name.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Product description.
    /// </summary>
    public string Description { get; set; } = "";

    /// <summary>
    /// Product image URL.
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Product page URL.
    /// </summary>
    public string? ProductUrl { get; set; }

    /// <summary>
    /// Quantity ordered.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Unit price before discounts.
    /// </summary>
    public Money UnitPrice { get; set; } = new();

    /// <summary>
    /// Unit price after discounts.
    /// </summary>
    public Money DiscountedUnitPrice { get; set; } = new();

    /// <summary>
    /// Total line price.
    /// </summary>
    public Money LineTotal { get; set; } = new();

    /// <summary>
    /// Tax amount for this item.
    /// </summary>
    public Money Tax { get; set; } = new();

    /// <summary>
    /// Tax rate applied.
    /// </summary>
    public decimal TaxRate { get; set; }

    /// <summary>
    /// Discount amount applied.
    /// </summary>
    public Money Discount { get; set; } = new();

    /// <summary>
    /// Weight per unit.
    /// </summary>
    public decimal? Weight { get; set; }

    /// <summary>
    /// Product variant attributes.
    /// </summary>
    public IDictionary<string, string> Attributes { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Custom data for this item.
    /// </summary>
    public IDictionary<string, object> CustomData { get; set; } = new Dictionary<string, object>();
}

#endregion
