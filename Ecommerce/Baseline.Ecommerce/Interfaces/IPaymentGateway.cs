using Microsoft.AspNetCore.Http;

namespace Baseline.Ecommerce;

/// <summary>
/// Represents the state of a payment transaction.
/// </summary>
public enum PaymentState
{
    /// <summary>Payment is pending and awaiting processing.</summary>
    Pending = 0,

    /// <summary>Payment is currently being processed.</summary>
    Processing = 1,

    /// <summary>Payment completed successfully.</summary>
    Succeeded = 2,

    /// <summary>Payment failed or was declined.</summary>
    Failed = 3,

    /// <summary>Payment was cancelled by user or system.</summary>
    Cancelled = 4,

    /// <summary>Payment was refunded (full or partial).</summary>
    Refunded = 5,

    /// <summary>Payment authorization was voided before capture.</summary>
    Voided = 6
}

/// <summary>
/// Line item for payment gateway display (product, discount, tax, etc.).
/// </summary>
/// <param name="Name">Display name of the line item.</param>
/// <param name="AmountMinor">Amount in minor currency units (negative for discounts).</param>
/// <param name="Quantity">Quantity (default 1 for fees/taxes/discounts).</param>
/// <param name="Note">Optional note or description for the line item.</param>
public sealed record LineItem(
    string Name,
    long AmountMinor,
    int Quantity = 1,
    string? Note = null);

/// <summary>
/// Snapshot of order data needed by payment gateways.
/// Immutable record for thread-safe handling.
/// </summary>
/// <param name="OrderNumber">Unique order identifier (used for tracking and webhooks).</param>
/// <param name="AmountMinor">Total amount in minor currency units (e.g., cents for USD).</param>
/// <param name="Currency">ISO 4217 currency code (e.g., "USD", "EUR").</param>
/// <param name="SuccessUrl">URL to redirect after successful payment.</param>
/// <param name="CancelUrl">URL to redirect if payment is cancelled.</param>
/// <param name="CustomerEmail">Optional customer email for receipt.</param>
/// <param name="CustomerName">Optional customer display name.</param>
/// <param name="Description">Optional payment description or order summary.</param>
/// <param name="Metadata">Optional key-value metadata to attach to payment.</param>
/// <param name="LineItems">Optional itemized breakdown (products only - positive amounts).</param>
/// <param name="SubtotalMinor">Optional subtotal amount in minor units (before discounts/taxes).</param>
/// <param name="DiscountMinor">Optional total discount amount in minor units.</param>
/// <param name="TaxMinor">Optional total tax amount in minor units.</param>
/// <param name="ShippingMinor">Optional shipping cost in minor units.</param>
public sealed record OrderSnapshot(
    string OrderNumber,
    long AmountMinor,
    string Currency,
    Uri SuccessUrl,
    Uri CancelUrl,
    string? CustomerEmail = null,
    string? CustomerName = null,
    string? Description = null,
    IReadOnlyDictionary<string, string>? Metadata = null,
    IReadOnlyList<LineItem>? LineItems = null,
    long? SubtotalMinor = null,
    long? DiscountMinor = null,
    long? TaxMinor = null,
    long? ShippingMinor = null);

/// <summary>
/// Result of creating a payment session.
/// </summary>
/// <param name="RedirectUrl">URL to redirect user for payment (hosted checkout).</param>
/// <param name="ProviderReference">Provider-specific reference ID (session ID, payment intent ID, etc.).</param>
public sealed record CreateSessionResult(string RedirectUrl, string ProviderReference);

/// <summary>
/// Result of processing a webhook from the payment provider.
/// </summary>
/// <param name="Handled">True if webhook was successfully processed.</param>
/// <param name="OrderNumber">Order number extracted from webhook, if applicable.</param>
/// <param name="State">Payment state determined from webhook event.</param>
/// <param name="ProviderReference">Provider reference from webhook (charge ID, etc.).</param>
public sealed record WebhookResult(
    bool Handled,
    string? OrderNumber,
    PaymentState? State = null,
    string? ProviderReference = null);

/// <summary>
/// Abstraction for payment gateway integrations (Stripe, Clover, Square, etc.).
/// Implementations handle provider-specific API calls and webhook verification.
/// </summary>
public interface IPaymentGateway
{
    /// <summary>
    /// Creates or reuses a payment session for the given order.
    /// Returns a redirect URL for hosted checkout flows.
    /// </summary>
    /// <param name="order">Order snapshot with amount and metadata.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Session result with redirect URL and provider reference.</returns>
    Task<CreateSessionResult> CreateOrReuseSessionAsync(OrderSnapshot order, CancellationToken ct = default);

    /// <summary>
    /// Handles incoming webhook from payment provider.
    /// Verifies signature and extracts order/payment information.
    /// </summary>
    /// <param name="request">HTTP request containing webhook payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Webhook result indicating if handled and order details.</returns>
    Task<WebhookResult> HandleWebhookAsync(HttpRequest request, CancellationToken ct = default);

    /// <summary>
    /// Captures a previously authorized payment.
    /// </summary>
    /// <param name="chargeId">Provider-specific charge/payment ID.</param>
    /// <param name="amountMinor">Optional partial capture amount. Null captures full amount.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if capture succeeded.</returns>
    Task<bool> CapturePaymentAsync(string chargeId, long? amountMinor = null, CancellationToken ct = default);

    /// <summary>
    /// Refunds a captured payment (full or partial).
    /// </summary>
    /// <param name="chargeId">Provider-specific charge/payment ID to refund.</param>
    /// <param name="amountMinor">Amount to refund in minor units. Null refunds full amount.</param>
    /// <param name="reason">Optional refund reason.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Refund ID if successful, null otherwise.</returns>
    Task<string?> RefundPaymentAsync(string chargeId, long? amountMinor = null, string? reason = null, CancellationToken ct = default);

    /// <summary>
    /// Voids (cancels) an uncaptured authorization.
    /// </summary>
    /// <param name="chargeId">Provider-specific charge/authorization ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if void succeeded.</returns>
    Task<bool> VoidPaymentAsync(string chargeId, CancellationToken ct = default);
}

/// <summary>
/// Service for updating order payment state in the application.
/// Implementations persist payment status to the order management system.
/// </summary>
public interface IOrderPayments
{
    /// <summary>
    /// Updates the payment state for an order.
    /// Called by payment gateway after webhook processing or direct API response.
    /// </summary>
    /// <param name="orderNumber">The order number/identifier.</param>
    /// <param name="state">The new payment state.</param>
    /// <param name="providerRef">Optional provider-specific reference (charge ID, transaction ID).</param>
    /// <param name="ct">Cancellation token.</param>
    Task SetStateAsync(string orderNumber, PaymentState state, string? providerRef = null, CancellationToken ct = default);
}
