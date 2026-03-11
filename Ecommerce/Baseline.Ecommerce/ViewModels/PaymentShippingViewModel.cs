using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Mvc.Rendering;

using static Baseline.Ecommerce.ViewModels.CheckoutFormConstants;

namespace Baseline.Ecommerce.ViewModels;

/// <summary>
/// ViewModel for payment and shipping method selection in checkout.
/// </summary>
public sealed record PaymentShippingViewModel
{
    public PaymentShippingViewModel()
    {
        PaymentMethod = ShippingMethod = string.Empty;
        Payments = [];
        Shippings = [];
    }

    public string PaymentMethod { get; set; }

    public string ShippingMethod { get; set; }

    public decimal ShippingPrice { get; set; }

    [Display(Name = "Payment method")]
    [Required(ErrorMessage = REQUIRED_FIELD_ERROR_MESSAGE)]
    public string? PaymentMethodId { get; set; }

    [Display(Name = "Shipping method")]
    // Note: Shipping method validation is handled dynamically in the controller based on product types
    public string? ShippingMethodId { get; set; }

    public IEnumerable<SelectListItem> Payments { get; set; }

    public IEnumerable<SelectListItem> Shippings { get; set; }

    /// <summary>
    /// Type of fulfillment for this order (e.g., "Digital Delivery", "Pickup", "Standard Shipping").
    /// </summary>
    public string? FulfillmentType { get; set; }

    public bool IsEmpty()
    {
        return string.IsNullOrEmpty(PaymentMethod) && string.IsNullOrEmpty(ShippingMethod);
    }
}
