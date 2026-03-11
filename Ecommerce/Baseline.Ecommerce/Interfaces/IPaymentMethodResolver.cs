namespace Baseline.Ecommerce;

/// <summary>
/// Service for resolving and managing available payment methods.
/// Determines which payment methods are available based on context such as
/// order amount, customer, shipping destination, and cart contents.
/// </summary>
public interface IPaymentMethodResolver
{
    /// <summary>
    /// Gets all available payment methods for the given context.
    /// </summary>
    /// <param name="context">The payment resolution context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Available payment methods ordered by priority.</returns>
    Task<IEnumerable<PaymentMethodOption>> GetAvailableMethodsAsync(
        PaymentMethodContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates if a specific payment method is available for the context.
    /// </summary>
    /// <param name="paymentMethodId">The payment method ID to validate.</param>
    /// <param name="context">The payment resolution context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Validation result with availability and any restrictions.</returns>
    Task<PaymentMethodValidationResult> ValidateMethodAsync(
        Guid paymentMethodId,
        PaymentMethodContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the default payment method for the context.
    /// </summary>
    /// <param name="context">The payment resolution context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The default payment method, or null if none available.</returns>
    Task<PaymentMethodOption?> GetDefaultMethodAsync(
        PaymentMethodContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets payment method by ID with full configuration.
    /// </summary>
    /// <param name="paymentMethodId">The payment method ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The payment method configuration, or null if not found.</returns>
    Task<PaymentMethodConfiguration?> GetMethodConfigurationAsync(
        Guid paymentMethodId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets saved payment methods for a customer.
    /// </summary>
    /// <param name="customerId">The customer ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Saved payment methods for the customer.</returns>
    Task<IEnumerable<SavedPaymentMethod>> GetSavedMethodsAsync(
        int customerId,
        CancellationToken cancellationToken = default);
}

#region Payment Method Models

/// <summary>
/// Context for payment method resolution.
/// </summary>
public class PaymentMethodContext
{
    /// <summary>
    /// The order total amount.
    /// </summary>
    public Money? OrderTotal { get; set; }

    /// <summary>
    /// The currency code.
    /// </summary>
    public string CurrencyCode { get; set; } = "";

    /// <summary>
    /// The billing country code.
    /// </summary>
    public string? BillingCountry { get; set; }

    /// <summary>
    /// The shipping country code.
    /// </summary>
    public string? ShippingCountry { get; set; }

    /// <summary>
    /// The customer ID (if authenticated).
    /// </summary>
    public int? CustomerId { get; set; }

    /// <summary>
    /// Whether this is a guest checkout.
    /// </summary>
    public bool IsGuestCheckout { get; set; }

    /// <summary>
    /// Whether the order includes subscriptions.
    /// </summary>
    public bool HasSubscriptionItems { get; set; }

    /// <summary>
    /// Whether the order includes digital-only items.
    /// </summary>
    public bool IsDigitalOnly { get; set; }

    /// <summary>
    /// The channel/website context.
    /// </summary>
    public string? ChannelName { get; set; }

    /// <summary>
    /// Available wallet balance for wallet payments.
    /// </summary>
    public Money? WalletBalance { get; set; }

    /// <summary>
    /// Additional context data.
    /// </summary>
    public IDictionary<string, object> AdditionalData { get; set; } = new Dictionary<string, object>();
}

/// <summary>
/// A payment method option available for selection.
/// </summary>
public class PaymentMethodOption
{
    /// <summary>
    /// Unique identifier for the payment method.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Payment method code (e.g., "CreditCard", "PayPal", "Wallet").
    /// </summary>
    public string Code { get; set; } = "";

    /// <summary>
    /// Display name for the payment method.
    /// </summary>
    public string DisplayName { get; set; } = "";

    /// <summary>
    /// Description of the payment method.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Icon URL or CSS class for the payment method.
    /// </summary>
    public string? IconUrl { get; set; }

    /// <summary>
    /// The type of payment method.
    /// </summary>
    public PaymentMethodType Type { get; set; }

    /// <summary>
    /// Whether this method is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Whether this is the default/recommended method.
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Whether this method requires a registered account.
    /// </summary>
    public bool RequiresAccount { get; set; }

    /// <summary>
    /// Whether this method supports recurring payments.
    /// </summary>
    public bool SupportsRecurring { get; set; } = true;

    /// <summary>
    /// Whether this method supports saving for future use.
    /// </summary>
    public bool SupportsSaving { get; set; }

    /// <summary>
    /// Whether this method supports refunds.
    /// </summary>
    public bool SupportsRefunds { get; set; }

    /// <summary>
    /// Whether this method supports partial payments.
    /// </summary>
    public bool SupportsPartialPayments { get; set; }

    /// <summary>
    /// Minimum order amount for this method.
    /// </summary>
    public decimal? MinimumAmount { get; set; }

    /// <summary>
    /// Maximum order amount for this method.
    /// </summary>
    public decimal? MaximumAmount { get; set; }

    /// <summary>
    /// Additional fee for using this method (if any).
    /// </summary>
    public Money? AdditionalFee { get; set; }

    /// <summary>
    /// Fee percentage for using this method (if any).
    /// </summary>
    public decimal? FeePercentage { get; set; }

    /// <summary>
    /// Display order/priority.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Supported currencies for this method.
    /// </summary>
    public IReadOnlyList<string> SupportedCurrencies { get; set; } = [];

    /// <summary>
    /// Supported countries for this method.
    /// </summary>
    public IReadOnlyList<string> SupportedCountries { get; set; } = [];

    /// <summary>
    /// Any restrictions or notes for this method.
    /// </summary>
    public IReadOnlyList<string> Restrictions { get; set; } = [];

    /// <summary>
    /// Accepted card brands (for card payments).
    /// </summary>
    public IReadOnlyList<string> AcceptedCardBrands { get; set; } = [];
}

/// <summary>
/// Type of payment method.
/// </summary>
public enum PaymentMethodType
{
    /// <summary>
    /// Credit card payment.
    /// </summary>
    CreditCard = 0,

    /// <summary>
    /// Debit card payment.
    /// </summary>
    DebitCard = 1,

    /// <summary>
    /// PayPal payment.
    /// </summary>
    PayPal = 2,

    /// <summary>
    /// Stripe payment.
    /// </summary>
    Stripe = 3,

    /// <summary>
    /// Bank transfer.
    /// </summary>
    BankTransfer = 4,

    /// <summary>
    /// Invoice/payment terms.
    /// </summary>
    Invoice = 5,

    /// <summary>
    /// Cash on delivery.
    /// </summary>
    CashOnDelivery = 6,

    /// <summary>
    /// Apple Pay.
    /// </summary>
    ApplePay = 7,

    /// <summary>
    /// Google Pay.
    /// </summary>
    GooglePay = 8,

    /// <summary>
    /// Digital wallet.
    /// </summary>
    Wallet = 9,

    /// <summary>
    /// Cryptocurrency payment.
    /// </summary>
    Cryptocurrency = 10,

    /// <summary>
    /// Gift card.
    /// </summary>
    GiftCard = 11,

    /// <summary>
    /// Store credit.
    /// </summary>
    StoreCredit = 12,

    /// <summary>
    /// Buy now, pay later.
    /// </summary>
    BuyNowPayLater = 13,

    /// <summary>
    /// Loyalty points redemption.
    /// </summary>
    LoyaltyPoints = 14,

    /// <summary>
    /// Other payment method.
    /// </summary>
    Other = 99
}

/// <summary>
/// Result of payment method validation.
/// </summary>
public record PaymentMethodValidationResult
{
    /// <summary>
    /// Whether the payment method is available.
    /// </summary>
    public bool IsAvailable { get; init; }

    /// <summary>
    /// The payment method if available.
    /// </summary>
    public PaymentMethodOption? PaymentMethod { get; init; }

    /// <summary>
    /// Reasons why the method is unavailable.
    /// </summary>
    public IReadOnlyList<string> UnavailableReasons { get; init; } = [];

    /// <summary>
    /// Warnings or restrictions that apply.
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = [];

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static PaymentMethodValidationResult Available(PaymentMethodOption method, IEnumerable<string>? warnings = null) =>
        new() { IsAvailable = true, PaymentMethod = method, Warnings = warnings?.ToList() ?? [] };

    /// <summary>
    /// Creates an unavailable result.
    /// </summary>
    public static PaymentMethodValidationResult Unavailable(params string[] reasons) =>
        new() { IsAvailable = false, UnavailableReasons = reasons };

    /// <summary>
    /// Creates a valid result.
    /// </summary>
    public static PaymentMethodValidationResult Valid(PaymentMethodOption? method = null) =>
        new() { IsAvailable = true, PaymentMethod = method };

    /// <summary>
    /// Creates an invalid result with error message.
    /// </summary>
    public static PaymentMethodValidationResult Invalid(string reason) =>
        new() { IsAvailable = false, UnavailableReasons = [reason] };
}

/// <summary>
/// Full configuration for a payment method.
/// </summary>
public class PaymentMethodConfiguration
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Payment method code.
    /// </summary>
    public string Code { get; set; } = "";

    /// <summary>
    /// Display name.
    /// </summary>
    public string DisplayName { get; set; } = "";

    /// <summary>
    /// Description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether the method is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// The payment method type.
    /// </summary>
    public PaymentMethodType Type { get; set; }

    /// <summary>
    /// Provider implementation class name.
    /// </summary>
    public string? ProviderType { get; set; }

    /// <summary>
    /// Whether sandbox/test mode is enabled.
    /// </summary>
    public bool IsSandbox { get; set; }

    /// <summary>
    /// API endpoint URL.
    /// </summary>
    public string? ApiUrl { get; set; }

    /// <summary>
    /// Provider-specific settings.
    /// </summary>
    public IDictionary<string, object> Settings { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// Countries where this method is available.
    /// </summary>
    public IReadOnlyList<string> AvailableCountries { get; set; } = [];

    /// <summary>
    /// Currencies supported by this method.
    /// </summary>
    public IReadOnlyList<string> SupportedCurrencies { get; set; } = [];
}

/// <summary>
/// A saved payment method for a customer.
/// </summary>
public class SavedPaymentMethod
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The customer ID.
    /// </summary>
    public int CustomerId { get; set; }

    /// <summary>
    /// The payment method type.
    /// </summary>
    public PaymentMethodType Type { get; set; }

    /// <summary>
    /// Display name (e.g., "Visa ending in 4242").
    /// </summary>
    public string DisplayName { get; set; } = "";

    /// <summary>
    /// Card brand (if applicable).
    /// </summary>
    public string? CardBrand { get; set; }

    /// <summary>
    /// Last 4 digits (if applicable).
    /// </summary>
    public string? Last4 { get; set; }

    /// <summary>
    /// Expiration month (if applicable).
    /// </summary>
    public int? ExpirationMonth { get; set; }

    /// <summary>
    /// Expiration year (if applicable).
    /// </summary>
    public int? ExpirationYear { get; set; }

    /// <summary>
    /// Whether this is the default method.
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// External token reference.
    /// </summary>
    public string? TokenReference { get; set; }

    /// <summary>
    /// Date when saved.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Whether this saved method is still valid.
    /// </summary>
    public bool IsValid => ExpirationYear == null ||
        new DateTime(ExpirationYear.Value, ExpirationMonth ?? 12, 1).AddMonths(1) > DateTime.Now;
}

#endregion
