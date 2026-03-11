using System.ComponentModel.DataAnnotations;

using static Baseline.Ecommerce.ViewModels.CheckoutFormConstants;

namespace Baseline.Ecommerce.ViewModels;

/// <summary>
/// ViewModel for shipping address information, extending customer address with same-as-billing option.
/// </summary>
public sealed record ShippingAddressViewModel : CustomerAddressViewModel
{
    [Display(Name = "Same as billing")]
    public bool IsSameAsBilling { get; set; }
}
