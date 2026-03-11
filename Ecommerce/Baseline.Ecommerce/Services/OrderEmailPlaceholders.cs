using CMS.Notifications;

namespace Baseline.Ecommerce;

/// <summary>
/// Notification email placeholders for order confirmation emails.
/// Create a notification email template named "OrderConfirmation" in XbK admin
/// using these placeholders to send branded order emails.
/// </summary>
public class OrderConfirmationPlaceholders : INotificationEmailPlaceholdersByCodeName
{
    public string NotificationEmailName => "OrderConfirmation";

    [PlaceholderRequired]
    [PlaceholderDescription("Order number")]
    public string OrderNumber { get; set; } = string.Empty;

    [PlaceholderRequired]
    [PlaceholderDescription("Customer first name")]
    public string FirstName { get; set; } = string.Empty;

    [PlaceholderRequired]
    [PlaceholderDescription("Customer last name")]
    public string LastName { get; set; } = string.Empty;

    [PlaceholderRequired]
    [PlaceholderDescription("Customer email address")]
    public string Email { get; set; } = string.Empty;

    [PlaceholderRequired]
    [PlaceholderDescription("Order date formatted as short date")]
    public string OrderDate { get; set; } = string.Empty;

    [PlaceholderRequired]
    [PlaceholderDescription("Formatted order total including currency symbol")]
    public string OrderTotal { get; set; } = string.Empty;

    [PlaceholderDescription("Formatted subtotal before tax")]
    public string Subtotal { get; set; } = string.Empty;

    [PlaceholderDescription("Formatted tax amount")]
    public string Tax { get; set; } = string.Empty;

    [PlaceholderDescription("Formatted shipping cost")]
    public string Shipping { get; set; } = string.Empty;

    [PlaceholderDescription("HTML table rows of order line items")]
    public string LineItemsHtml { get; set; } = string.Empty;

    [PlaceholderDescription("Shipping address formatted as single line")]
    public string ShippingAddress { get; set; } = string.Empty;

    [PlaceholderDescription("Billing address formatted as single line")]
    public string BillingAddress { get; set; } = string.Empty;

    [PlaceholderDescription("Payment method name (e.g. Visa ending 1234)")]
    public string PaymentMethod { get; set; } = string.Empty;

    [PlaceholderDescription("Current order status label")]
    public string OrderStatus { get; set; } = string.Empty;
}

/// <summary>
/// Notification email placeholders for shipping confirmation emails.
/// Create a notification email template named "ShippingConfirmation" in XbK admin.
/// </summary>
public class ShippingConfirmationPlaceholders : INotificationEmailPlaceholdersByCodeName
{
    public string NotificationEmailName => "ShippingConfirmation";

    [PlaceholderRequired]
    [PlaceholderDescription("Order number")]
    public string OrderNumber { get; set; } = string.Empty;

    [PlaceholderRequired]
    [PlaceholderDescription("Customer first name")]
    public string FirstName { get; set; } = string.Empty;

    [PlaceholderRequired]
    [PlaceholderDescription("Tracking number")]
    public string TrackingNumber { get; set; } = string.Empty;

    [PlaceholderDescription("Shipping carrier name")]
    public string Carrier { get; set; } = string.Empty;

    [PlaceholderDescription("Tracking URL")]
    public string TrackingUrl { get; set; } = string.Empty;

    [PlaceholderDescription("Estimated delivery date")]
    public string EstimatedDelivery { get; set; } = string.Empty;

    [PlaceholderDescription("Shipping address formatted as single line")]
    public string ShippingAddress { get; set; } = string.Empty;
}

/// <summary>
/// Notification email placeholders for order cancellation emails.
/// Create a notification email template named "OrderCancellation" in XbK admin.
/// </summary>
public class OrderCancellationPlaceholders : INotificationEmailPlaceholdersByCodeName
{
    public string NotificationEmailName => "OrderCancellation";

    [PlaceholderRequired]
    [PlaceholderDescription("Order number")]
    public string OrderNumber { get; set; } = string.Empty;

    [PlaceholderRequired]
    [PlaceholderDescription("Customer first name")]
    public string FirstName { get; set; } = string.Empty;

    [PlaceholderDescription("Cancellation reason")]
    public string Reason { get; set; } = string.Empty;

    [PlaceholderDescription("Formatted order total that was cancelled")]
    public string OrderTotal { get; set; } = string.Empty;
}

/// <summary>
/// Notification email placeholders for refund confirmation emails.
/// Create a notification email template named "RefundConfirmation" in XbK admin.
/// </summary>
public class RefundConfirmationPlaceholders : INotificationEmailPlaceholdersByCodeName
{
    public string NotificationEmailName => "RefundConfirmation";

    [PlaceholderRequired]
    [PlaceholderDescription("Order number")]
    public string OrderNumber { get; set; } = string.Empty;

    [PlaceholderRequired]
    [PlaceholderDescription("Customer first name")]
    public string FirstName { get; set; } = string.Empty;

    [PlaceholderRequired]
    [PlaceholderDescription("Formatted refund amount with currency")]
    public string RefundAmount { get; set; } = string.Empty;

    [PlaceholderDescription("Formatted original order total")]
    public string OrderTotal { get; set; } = string.Empty;

    [PlaceholderDescription("Payment method refunded to")]
    public string PaymentMethod { get; set; } = string.Empty;
}
