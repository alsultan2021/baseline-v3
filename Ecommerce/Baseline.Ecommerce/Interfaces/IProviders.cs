namespace Baseline.Ecommerce;

/// <summary>
/// Payment provider interface for extensible payment processing.
/// Implement this interface to add a new payment method.
/// </summary>
public interface IPaymentProvider
{
    /// <summary>
    /// Unique identifier for this provider.
    /// </summary>
    string ProviderId { get; }

    /// <summary>
    /// Display name for the payment method.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Description of the payment method.
    /// </summary>
    string? Description { get; }

    /// <summary>
    /// Icon URL for the payment method.
    /// </summary>
    string? IconUrl { get; }

    /// <summary>
    /// Priority for ordering (higher = earlier in list).
    /// </summary>
    int Priority => 0;

    /// <summary>
    /// Checks if this provider is available for the current checkout.
    /// </summary>
    Task<bool> IsAvailableAsync(CheckoutSession session);

    /// <summary>
    /// Initializes a payment.
    /// </summary>
    Task<PaymentInitResult> InitializePaymentAsync(PaymentInitRequest request);

    /// <summary>
    /// Processes the payment.
    /// </summary>
    Task<PaymentResult> ProcessPaymentAsync(PaymentProcessRequest request);

    /// <summary>
    /// Handles a payment callback/webhook.
    /// </summary>
    Task<PaymentCallbackResult> HandleCallbackAsync(PaymentCallbackRequest request);

    /// <summary>
    /// Refunds a payment.
    /// </summary>
    Task<RefundResult> RefundAsync(RefundRequest request);

    /// <summary>
    /// Gets the status of a payment.
    /// </summary>
    Task<PaymentStatus> GetPaymentStatusAsync(string paymentId);
}

/// <summary>
/// Shipping provider interface for extensible shipping calculation.
/// </summary>
public interface IShippingProvider
{
    /// <summary>
    /// Unique identifier for this provider.
    /// </summary>
    string ProviderId { get; }

    /// <summary>
    /// Display name for the shipping provider.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Priority for ordering (higher = earlier in list).
    /// </summary>
    int Priority => 0;

    /// <summary>
    /// Checks if this provider is available for the given address.
    /// </summary>
    Task<bool> IsAvailableAsync(Address shippingAddress);

    /// <summary>
    /// Gets available shipping methods from this provider.
    /// </summary>
    Task<IEnumerable<ShippingMethodOption>> GetMethodsAsync(ShippingCalculationRequest request);

    /// <summary>
    /// Calculates shipping cost.
    /// </summary>
    Task<ShippingCost> CalculateAsync(ShippingCalculationRequest request, string methodId);

    /// <summary>
    /// Creates a shipment (if applicable).
    /// </summary>
    Task<ShipmentResult> CreateShipmentAsync(ShipmentRequest request);

    /// <summary>
    /// Tracks a shipment.
    /// </summary>
    Task<ShipmentTracking?> TrackAsync(string trackingNumber);
}

/// <summary>
/// Service for discovering and managing payment providers.
/// </summary>
public interface IPaymentProviderRegistry
{
    /// <summary>
    /// Gets all registered payment providers.
    /// </summary>
    IEnumerable<IPaymentProvider> GetProviders();

    /// <summary>
    /// Gets a specific provider by ID.
    /// </summary>
    IPaymentProvider? GetProvider(string providerId);

    /// <summary>
    /// Gets available providers for a checkout session.
    /// </summary>
    Task<IEnumerable<IPaymentProvider>> GetAvailableProvidersAsync(CheckoutSession session);
}

/// <summary>
/// Service for discovering and managing shipping providers.
/// </summary>
public interface IShippingProviderRegistry
{
    /// <summary>
    /// Gets all registered shipping providers.
    /// </summary>
    IEnumerable<IShippingProvider> GetProviders();

    /// <summary>
    /// Gets a specific provider by ID.
    /// </summary>
    IShippingProvider? GetProvider(string providerId);

    /// <summary>
    /// Gets available providers for an address.
    /// </summary>
    Task<IEnumerable<IShippingProvider>> GetAvailableProvidersAsync(Address shippingAddress);
}

#region Payment Models

/// <summary>
/// Payment initialization request.
/// </summary>
public class PaymentInitRequest
{
    public Guid OrderId { get; set; }
    public Money Amount { get; set; } = Money.Zero();
    public string Currency { get; set; } = "USD";
    public string? ReturnUrl { get; set; }
    public string? CancelUrl { get; set; }
    public string? WebhookUrl { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = [];
}

/// <summary>
/// Result of payment initialization.
/// </summary>
public record PaymentInitResult
{
    public bool Success { get; init; }
    public string? PaymentId { get; init; }
    public string? RedirectUrl { get; init; }
    public string? ClientSecret { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Payment processing request.
/// </summary>
public class PaymentProcessRequest
{
    public string PaymentId { get; set; } = string.Empty;
    public Guid OrderId { get; set; }
    public Dictionary<string, string> PaymentData { get; set; } = [];
}

/// <summary>
/// Result of payment processing.
/// </summary>
public record PaymentResult
{
    public bool Success { get; init; }
    public string? TransactionId { get; init; }
    public PaymentStatus Status { get; init; }
    public string? ErrorMessage { get; init; }
    public string? ErrorCode { get; init; }
}

/// <summary>
/// Payment callback/webhook request.
/// </summary>
public class PaymentCallbackRequest
{
    public string RawBody { get; set; } = string.Empty;
    public Dictionary<string, string> Headers { get; set; } = [];
    public Dictionary<string, string> QueryParams { get; set; } = [];
}

/// <summary>
/// Result of payment callback processing.
/// </summary>
public record PaymentCallbackResult
{
    public bool Success { get; init; }
    public string? PaymentId { get; init; }
    public PaymentStatus Status { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Refund request.
/// </summary>
public class RefundRequest
{
    public string PaymentId { get; set; } = string.Empty;
    public Money? Amount { get; set; }
    public string? Reason { get; set; }
}

/// <summary>
/// Result of refund processing.
/// </summary>
public record RefundResult
{
    public bool Success { get; init; }
    public string? RefundId { get; init; }
    public Money? RefundedAmount { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Payment status.
/// </summary>
public enum PaymentStatus
{
    Pending,
    Processing,
    Authorized,
    Captured,
    Completed,
    Failed,
    Cancelled,
    Refunded,
    PartiallyRefunded
}

#endregion

#region Shipping Models

/// <summary>
/// Shipping calculation request.
/// Uses <see cref="ShippingItem"/> from IShippingServices.
/// </summary>
public class ShippingCalculationRequest
{
    public Address ShippingAddress { get; set; } = new();
    public IEnumerable<ShippingItem> Items { get; set; } = [];
    public Money CartSubtotal { get; set; } = Money.Zero();
}

// Note: ShippingItem is defined in IShippingServices.cs with additional properties:
// - FreeShipping, RequiresSpecialHandling, IsFragile, Dimensions

/// <summary>
/// Shipping method option.
/// </summary>
public class ShippingMethodOption
{
    public string MethodId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Money Cost { get; set; } = Money.Zero();
    public int? EstimatedDaysMin { get; set; }
    public int? EstimatedDaysMax { get; set; }
    public string? CarrierName { get; set; }
}

/// <summary>
/// Shipping cost result.
/// </summary>
public record ShippingCost
{
    public Money Amount { get; init; } = Money.Zero();
    public int? EstimatedDaysMin { get; init; }
    public int? EstimatedDaysMax { get; init; }
}

/// <summary>
/// Shipment creation request.
/// </summary>
public class ShipmentRequest
{
    public Guid OrderId { get; set; }
    public Address FromAddress { get; set; } = new();
    public Address ToAddress { get; set; } = new();
    public IEnumerable<ShippingItem> Items { get; set; } = [];
    public string ServiceType { get; set; } = string.Empty;
}

/// <summary>
/// Shipment creation result.
/// </summary>
public record ShipmentResult
{
    public bool Success { get; init; }
    public string? ShipmentId { get; init; }
    public string? TrackingNumber { get; init; }
    public string? LabelUrl { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Shipment tracking information.
/// </summary>
public class ShipmentTracking
{
    public string TrackingNumber { get; set; } = string.Empty;
    public string Carrier { get; set; } = string.Empty;
    public ShipmentStatus Status { get; set; }
    public DateTimeOffset? EstimatedDelivery { get; set; }
    public IEnumerable<TrackingEvent> Events { get; set; } = [];
}

/// <summary>
/// Shipment status.
/// </summary>
public enum ShipmentStatus
{
    LabelCreated,
    InTransit,
    OutForDelivery,
    Delivered,
    Exception,
    Returned
}

/// <summary>
/// Tracking event.
/// </summary>
public class TrackingEvent
{
    public DateTimeOffset Timestamp { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Location { get; set; }
    public ShipmentStatus Status { get; set; }
}

#endregion
