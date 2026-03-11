using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;

using static Baseline.Ecommerce.ViewModels.CheckoutFormConstants;

namespace Baseline.Ecommerce.ViewModels;

/// <summary>
/// ViewModel for customer address information in checkout forms.
/// </summary>
public record CustomerAddressViewModel
{
    public CustomerAddressViewModel()
    {
        Line1 = string.Empty;
        Line2 = string.Empty;
        City = string.Empty;
        PostalCode = string.Empty;
        CountryId = string.Empty;
        StateId = string.Empty;
        Country = string.Empty;
        State = string.Empty;
        Countries = [];
        States = [];
    }

    public CustomerAddressViewModel(IEnumerable<SelectListItem> countries) : this()
    {
        Countries = countries;
    }

    [Display(Name = "Street address")]
    [Required(ErrorMessage = REQUIRED_FIELD_ERROR_MESSAGE)]
    [MaxLength(200, ErrorMessage = MAX_LENGTH_ERROR_MESSAGE)]
    public string Line1 { get; set; }

    [Display(Name = "Apartment, suite, unit, etc.")]
    [MaxLength(200, ErrorMessage = MAX_LENGTH_ERROR_MESSAGE)]
    public string Line2 { get; set; }

    [Display(Name = "City")]
    [Required(ErrorMessage = REQUIRED_FIELD_ERROR_MESSAGE)]
    [MaxLength(100, ErrorMessage = MAX_LENGTH_ERROR_MESSAGE)]
    public string City { get; set; }

    [Display(Name = "Postal code")]
    [Required(ErrorMessage = REQUIRED_FIELD_ERROR_MESSAGE)]
    [MaxLength(10, ErrorMessage = MAX_LENGTH_ERROR_MESSAGE)]
    public string PostalCode { get; set; }

    [Display(Name = "Country")]
    [Required(ErrorMessage = REQUIRED_FIELD_ERROR_MESSAGE)]
    public string CountryId { get; set; }

    [Display(Name = "State")]
    public string StateId { get; set; }

    [ScaffoldColumn(false)]
    [BindNever]
    public string Country { get; set; }

    [ScaffoldColumn(false)]
    [BindNever]
    public string State { get; set; }

    [BindNever]
    public IEnumerable<SelectListItem> Countries { get; set; }

    [BindNever]
    public IEnumerable<SelectListItem> States { get; set; }
}
