namespace Baseline.Ecommerce;

/// <summary>
/// Order information.
/// </summary>
public class Order
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public int? UserId { get; set; }
    public OrderStatus Status { get; set; }
    public IList<OrderItem> Items { get; set; } = [];
    public Address ShippingAddress { get; set; } = new();
    public Address BillingAddress { get; set; } = new();
    public ShippingMethod ShippingMethod { get; set; } = new();
    public string PaymentMethod { get; set; } = string.Empty;
    public CartTotals Totals { get; set; } = new();
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ShippedAt { get; set; }
    public DateTimeOffset? DeliveredAt { get; set; }
    public string? TrackingNumber { get; set; }
}

/// <summary>
/// Order item.
/// </summary>
public class OrderItem
{
    public Guid Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public int Quantity { get; set; }
    public Money UnitPrice { get; set; } = new();
    public Money LineTotal { get; set; } = new();
}

/// <summary>
/// Order status.
/// </summary>
public enum OrderStatus
{
    Pending,
    PaymentReceived,
    Paid,
    Processing,
    Shipped,
    Delivered,
    Cancelled,
    Refunded
}

/// <summary>
/// Order summary for lists.
/// </summary>
public class OrderSummary
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public int ItemCount { get; set; }
    public Money Total { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>
/// Result of an order operation.
/// </summary>
public record OrderResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public Order? Order { get; init; }
    public string? RedirectUrl { get; init; }

    public static OrderResult Succeeded(Order order, string? redirectUrl = null) =>
        new() { Success = true, Order = order, RedirectUrl = redirectUrl };
    public static OrderResult Failed(string message) =>
        new() { Success = false, ErrorMessage = message };
}

/// <summary>
/// Request to create a new order with all checkout data and price calculation results.
/// Includes promotion data for persistence.
/// </summary>
public record CreateOrderRequest
{
    /// <summary>
    /// Billing address for the order.
    /// </summary>
    public required Address BillingAddress { get; init; }

    /// <summary>
    /// Shipping address for the order. If null, billing address is used.
    /// </summary>
    public Address? ShippingAddress { get; init; }

    /// <summary>
    /// Cart items to include in the order.
    /// </summary>
    public required IReadOnlyList<CartItem> Items { get; init; }

    /// <summary>
    /// Selected shipping method ID.
    /// </summary>
    public Guid? ShippingMethodId { get; init; }

    /// <summary>
    /// Selected payment method ID.
    /// </summary>
    public Guid? PaymentMethodId { get; init; }

    /// <summary>
    /// Price calculation result containing totals and applied promotions.
    /// </summary>
    public required PriceCalculationResult PriceCalculation { get; init; }

    /// <summary>
    /// Optional order notes from the customer.
    /// </summary>
    public string? Notes { get; init; }

    /// <summary>
    /// Language code for the order.
    /// </summary>
    public string Language { get; init; } = "en";

    /// <summary>
    /// Member ID if the customer is authenticated.
    /// </summary>
    public int? MemberId { get; init; }
}
