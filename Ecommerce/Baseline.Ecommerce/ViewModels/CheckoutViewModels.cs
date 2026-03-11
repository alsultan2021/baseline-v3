using Baseline.Core;
using Baseline.Ecommerce.Models;

namespace Baseline.Ecommerce.ViewModels;

/// <summary>
/// Main checkout form ViewModel containing all customer, address, cart, and payment information.
/// </summary>
public sealed record CheckoutViewModel(
    CheckoutStep Step,
    CustomerViewModel Customer,
    CustomerAddressViewModel BillingAddress,
    ShippingAddressViewModel ShippingAddress,
    ShoppingCartViewModel ShoppingCart,
    PaymentShippingViewModel PaymentShipping)
{
    /// <summary>
    /// Indicates whether this checkout requires shipping based on the product types in the cart.
    /// </summary>
    public bool RequiresShipping { get; init; } = true;
}

/// <summary>
/// View model for the checkout page including page metadata and auth URLs.
/// </summary>
public record CheckoutPageViewModel(
    PageIdentity? Page,
    CheckoutViewModel? CheckoutForm,
    string LoginUrl = "",
    string ForgotPasswordUrl = "",
    string RegisterUrl = "",
    string MyAccountUrl = "");

/// <summary>
/// View model for the order confirmation page.
/// </summary>
public record OrderConfirmationPageViewModel(
    PageIdentity? Page,
    CustomerViewModel Customer,
    CustomerAddressViewModel CustomerAddress);

/// <summary>
/// View model for order confirmation result.
/// </summary>
public record OrderConfirmationResultViewModel(
    bool IsSuccess,
    string OrderNumber,
    string ErrorMessage)
{
    public bool HasError => !IsSuccess && !string.IsNullOrEmpty(ErrorMessage);
}

/// <summary>
/// Represents a single discount entry for display.
/// </summary>
public record DiscountEntry(string Name, decimal Amount)
{
    public string FormattedAmount => Amount.ToString("C");
}

/// <summary>
/// Represents a single tax entry for display (e.g., GST, QST as separate lines).
/// </summary>
public record TaxEntry(string Name, decimal Rate, decimal Amount)
{
    public string FormattedRate => $"{Rate * 100:F3}";
    public string FormattedAmount => Amount.ToString("C");
    public string FormattedLabel => $"{Name} ({FormattedRate}%)";
}

/// <summary>
/// Represents an applied coupon/discount code for UI state hydration.
/// </summary>
public record AppliedCouponViewModel(string Code, string Description);

/// <summary>
/// Shopping cart ViewModel for display in checkout and cart pages.
/// </summary>
public record ShoppingCartViewModel(
    ICollection<ShoppingCartItemViewModel> Items,
    decimal Subtotal,
    decimal TotalPrice,
    decimal Tax = 0,
    decimal TaxRate = 0,
    string TaxName = "Tax",
    decimal Discount = 0,
    string? DiscountDescription = null,
    ICollection<DiscountEntry>? DiscountEntries = null,
    ICollection<TaxEntry>? TaxEntries = null,
    ICollection<AppliedCouponViewModel>? AppliedCoupons = null)
{
    public ICollection<ShoppingCartItemViewModel> CartItems => Items;
    public int ItemCount => Items.Sum(item => item.Quantity);

    /// <summary>
    /// The original subtotal before any discounts are applied.
    /// </summary>
    public string FormattedSubtotal => Subtotal.ToString("C");

    /// <summary>
    /// The subtotal after all discounts (gift cards, promotions) but before taxes.
    /// This is: GrandTotal - Tax = (Subtotal - Discounts).
    /// </summary>
    public decimal SubtotalAfterDiscounts => TotalPrice - Tax;
    public string FormattedSubtotalAfterDiscounts => SubtotalAfterDiscounts.ToString("C");

    public string FormattedTotalPrice => TotalPrice.ToString("C");
    public string FormattedTax => Tax.ToString("C");
    public string FormattedTaxRate => $"{TaxRate * 100:F3}";
    public string FormattedTaxLabel => TaxRate == 0m || Tax == 0m
        ? TaxName // Just show the name (e.g., "Tax Exempt") when no tax applied
        : $"{TaxName} ({FormattedTaxRate}%)";
    public string FormattedDiscount => Discount.ToString("C");
    public bool HasDiscount => Discount > 0;
    public bool HasMultipleDiscounts => DiscountEntries?.Count > 1;
    public bool HasMultipleTaxes => TaxEntries?.Count > 1;
    public bool IsEmpty => !Items.Any();
}

/// <summary>
/// Individual cart item ViewModel.
/// </summary>
public record ShoppingCartItemViewModel(
    int ContentItemId,
    string Name,
    string? ImageUrl,
    string? DetailUrl,
    int Quantity,
    decimal UnitPrice,
    decimal TotalPrice,
    int? VariantId,
    bool IsTicket = false)
{
    // Alias properties for backward compatibility with views
    public string ProductName => Name;
    public string? ProductUrl => DetailUrl;
    public int Id => ContentItemId;

    // Formatted price properties
    public string FormattedUnitPrice => UnitPrice.ToString("C");
    public string FormattedTotalPrice => TotalPrice.ToString("C");
}
