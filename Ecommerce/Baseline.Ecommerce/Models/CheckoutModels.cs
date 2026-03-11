namespace Baseline.Ecommerce;

/// <summary>
/// Checkout session state.
/// </summary>
public class CheckoutSession
{
    public Guid Id { get; set; }
    public Guid CartId { get; set; }
    public Address? ShippingAddress { get; set; }
    public Address? BillingAddress { get; set; }
    public bool UseSameAddressForBilling { get; set; } = true;
    public Guid? ShippingMethodId { get; set; }
    public Guid? PaymentMethodId { get; set; }
    public string CurrentStep { get; set; } = "cart";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}

/// <summary>
/// Address information.
/// </summary>
public class Address
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Company { get; set; }
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string? StateProvince { get; set; }
    public string PostalCode { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();
}

/// <summary>
/// Shipping method information.
/// </summary>
public class ShippingMethod
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Money Cost { get; set; } = Money.Zero();
    public string? EstimatedDelivery { get; set; }
    public string? Carrier { get; set; }
}

/// <summary>
/// Payment method information.
/// </summary>
public class PaymentMethod
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = string.Empty; // CreditCard, PayPal, etc.
    public string? IconUrl { get; set; }
    public bool RequiresRedirect { get; set; }
}

/// <summary>
/// Result of a checkout operation.
/// </summary>
public record CheckoutResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public CheckoutSession? Session { get; init; }

    public static CheckoutResult Succeeded(CheckoutSession session) =>
        new() { Success = true, Session = session };
    public static CheckoutResult Failed(string message) =>
        new() { Success = false, ErrorMessage = message };
}

/// <summary>
/// Request to complete checkout.
/// </summary>
public record CompleteCheckoutRequest
{
    public string? OrderNotes { get; init; }
    public bool AcceptedTerms { get; init; }
    public Dictionary<string, string>? PaymentData { get; init; }
}
