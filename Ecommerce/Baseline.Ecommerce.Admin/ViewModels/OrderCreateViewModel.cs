using Kentico.Xperience.Admin.Base.FormAnnotations;
using Baseline.Ecommerce.Admin.DataProviders;

namespace Baseline.Ecommerce.Admin.ViewModels;

/// <summary>
/// View model for manually creating orders from the admin UI.
/// Supports phone orders and administrative order creation.
/// </summary>
public class OrderCreateViewModel
{
    // Customer Information Section
    [TextInputComponent(
        Label = "First Name",
        ExplanationText = "Customer's first name",
        Order = 10)]
    [RequiredValidationRule(ErrorMessage = "First name is required")]
    public string FirstName { get; set; } = string.Empty;

    [TextInputComponent(
        Label = "Last Name",
        ExplanationText = "Customer's last name",
        Order = 20)]
    [RequiredValidationRule(ErrorMessage = "Last name is required")]
    public string LastName { get; set; } = string.Empty;

    [TextInputComponent(
        Label = "Email",
        ExplanationText = "Customer's email address",
        Order = 30)]
    [RequiredValidationRule(ErrorMessage = "Email is required")]
    [EmailValidationRule]
    public string Email { get; set; } = string.Empty;

    [TextInputComponent(
        Label = "Phone Number",
        ExplanationText = "Customer's phone number (optional)",
        Order = 40)]
    public string? PhoneNumber { get; set; }

    [TextInputComponent(
        Label = "Company",
        ExplanationText = "Company name (optional)",
        Order = 50)]
    public string? Company { get; set; }

    // Billing Address Section
    [TextInputComponent(
        Label = "Billing Address Line 1",
        ExplanationText = "Street address",
        Order = 100)]
    [RequiredValidationRule(ErrorMessage = "Billing address is required")]
    public string BillingLine1 { get; set; } = string.Empty;

    [TextInputComponent(
        Label = "Billing Address Line 2",
        ExplanationText = "Apartment, suite, etc. (optional)",
        Order = 110)]
    public string? BillingLine2 { get; set; }

    [TextInputComponent(
        Label = "Billing City",
        Order = 120)]
    [RequiredValidationRule(ErrorMessage = "City is required")]
    public string BillingCity { get; set; } = string.Empty;

    [TextInputComponent(
        Label = "Billing State/Province",
        Order = 130)]
    public string? BillingState { get; set; }

    [TextInputComponent(
        Label = "Billing Postal Code",
        Order = 140)]
    [RequiredValidationRule(ErrorMessage = "Postal code is required")]
    public string BillingPostalCode { get; set; } = string.Empty;

    [DropDownComponent(
        Label = "Billing Country",
        DataProviderType = typeof(CountryDataProvider),
        Order = 150)]
    [RequiredValidationRule(ErrorMessage = "Country is required")]
    public string BillingCountryID { get; set; } = string.Empty;

    // Shipping Address Section
    [CheckBoxComponent(
        Label = "Shipping address same as billing",
        Order = 200)]
    public bool ShippingSameAsBilling { get; set; } = true;

    [TextInputComponent(
        Label = "Shipping Address Line 1",
        ExplanationText = "Street address",
        Order = 210)]
    [VisibleIfFalse(nameof(ShippingSameAsBilling))]
    public string? ShippingLine1 { get; set; }

    [TextInputComponent(
        Label = "Shipping Address Line 2",
        Order = 220)]
    [VisibleIfFalse(nameof(ShippingSameAsBilling))]
    public string? ShippingLine2 { get; set; }

    [TextInputComponent(
        Label = "Shipping City",
        Order = 230)]
    [VisibleIfFalse(nameof(ShippingSameAsBilling))]
    public string? ShippingCity { get; set; }

    [TextInputComponent(
        Label = "Shipping State/Province",
        Order = 240)]
    [VisibleIfFalse(nameof(ShippingSameAsBilling))]
    public string? ShippingState { get; set; }

    [TextInputComponent(
        Label = "Shipping Postal Code",
        Order = 250)]
    [VisibleIfFalse(nameof(ShippingSameAsBilling))]
    public string? ShippingPostalCode { get; set; }

    [DropDownComponent(
        Label = "Shipping Country",
        DataProviderType = typeof(CountryDataProvider),
        Order = 260)]
    [VisibleIfFalse(nameof(ShippingSameAsBilling))]
    public string? ShippingCountryID { get; set; }

    // Order Items Section
    [TextAreaComponent(
        Label = "Order Items (JSON)",
        ExplanationText = "Enter order items as JSON array: [{\"sku\":\"SKU001\",\"name\":\"Product Name\",\"quantity\":1,\"unitPrice\":29.99}]",
        Order = 300)]
    [RequiredValidationRule(ErrorMessage = "At least one order item is required")]
    public string OrderItemsJson { get; set; } = "[]";

    // Shipping & Payment Section
    [DropDownComponent(
        Label = "Shipping Method",
        DataProviderType = typeof(ShippingMethodDataProvider),
        Order = 400)]
    public string? ShippingMethodID { get; set; }

    [DropDownComponent(
        Label = "Payment Method",
        DataProviderType = typeof(PaymentMethodDataProvider),
        Order = 410)]
    [RequiredValidationRule(ErrorMessage = "Payment method is required")]
    public string PaymentMethodID { get; set; } = string.Empty;

    [DropDownComponent(
        Label = "Order Status",
        DataProviderType = typeof(OrderStatusDataProvider),
        Order = 420)]
    [RequiredValidationRule(ErrorMessage = "Order status is required")]
    public string OrderStatusID { get; set; } = string.Empty;

    // Additional Information
    [TextAreaComponent(
        Label = "Internal Notes",
        ExplanationText = "Admin notes about this order (not visible to customer)",
        Order = 500)]
    public string? InternalNotes { get; set; }

    [CheckBoxComponent(
        Label = "Send order confirmation email",
        Order = 510)]
    public bool SendConfirmationEmail { get; set; } = true;

    [CheckBoxComponent(
        Label = "Mark as paid",
        ExplanationText = "Check if payment has already been received (e.g., cash or check payment)",
        Order = 520)]
    public bool MarkAsPaid { get; set; }
}

/// <summary>
/// Represents a single order item for JSON parsing.
/// </summary>
public class OrderItemInput
{
    public string SKU { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
}
