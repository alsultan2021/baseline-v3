using System.ComponentModel.DataAnnotations;

using Baseline.Ecommerce.Models;

using static Baseline.Ecommerce.ViewModels.CheckoutFormConstants;

namespace Baseline.Ecommerce.ViewModels;

/// <summary>
/// ViewModel for customer personal information in checkout forms.
/// </summary>
public sealed record CustomerViewModel
{
    public CustomerViewModel() => FirstName = LastName = Email = PhoneNumber = Company = string.Empty;

    public CustomerViewModel(string firstName, string lastName, string email, string phoneNumber)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        PhoneNumber = phoneNumber;
        Company = string.Empty;
    }

    [Display(Name = "First name")]
    [Required(ErrorMessage = REQUIRED_FIELD_ERROR_MESSAGE)]
    [MaxLength(100, ErrorMessage = MAX_LENGTH_ERROR_MESSAGE)]
    public string FirstName { get; set; }

    [Display(Name = "Last name")]
    [Required(ErrorMessage = REQUIRED_FIELD_ERROR_MESSAGE)]
    [MaxLength(200, ErrorMessage = MAX_LENGTH_ERROR_MESSAGE)]
    public string LastName { get; set; }

    [Display(Name = "Company")]
    [MaxLength(200, ErrorMessage = MAX_LENGTH_ERROR_MESSAGE)]
    public string Company { get; set; }

    [Display(Name = "Email")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [Required(ErrorMessage = REQUIRED_FIELD_ERROR_MESSAGE)]
    [MaxLength(255, ErrorMessage = MAX_LENGTH_ERROR_MESSAGE)]
    public string Email { get; set; }

    [Display(Name = "Phone")]
    [Required(ErrorMessage = REQUIRED_FIELD_ERROR_MESSAGE)]
    [Phone(ErrorMessage = "Please enter a valid phone number.")]
    [MaxLength(30, ErrorMessage = MAX_LENGTH_ERROR_MESSAGE)]
    public string PhoneNumber { get; set; }

    /// <summary>
    /// Checks whether all fields are empty.
    /// </summary>
    public bool IsEmpty() => string.IsNullOrEmpty(FirstName) && string.IsNullOrEmpty(LastName) && string.IsNullOrEmpty(Company) && string.IsNullOrEmpty(Email) && string.IsNullOrEmpty(PhoneNumber);

    /// <summary>
    /// Converts this ViewModel to a CustomerDto for backend processing.
    /// </summary>
    public CustomerDto ToCustomerDto(CustomerAddressViewModel billingAddressViewModel, ShippingAddressViewModel shippingAddressViewModel)
    {
        int.TryParse(billingAddressViewModel.CountryId, out int billingCountryId);
        int.TryParse(billingAddressViewModel.StateId, out int billingStateId);
        int.TryParse(shippingAddressViewModel.CountryId, out int shippingCountryId);
        int.TryParse(shippingAddressViewModel.StateId, out int shippingStateId);

        return new CustomerDto
        {
            FirstName = FirstName,
            LastName = LastName,
            Email = Email,
            PhoneNumber = PhoneNumber,

            Company = Company,
            BillingAddressLine1 = billingAddressViewModel.Line1,
            BillingAddressLine2 = billingAddressViewModel.Line2,
            BillingAddressCity = billingAddressViewModel.City,
            BillingAddressPostalCode = billingAddressViewModel.PostalCode,
            BillingAddressCountryId = billingCountryId,
            BillingAddressStateId = billingStateId,

            ShippingAddressLine1 = shippingAddressViewModel.IsSameAsBilling ? billingAddressViewModel.Line1 : shippingAddressViewModel.Line1,
            ShippingAddressLine2 = shippingAddressViewModel.IsSameAsBilling ? billingAddressViewModel.Line2 : shippingAddressViewModel.Line2,
            ShippingAddressCity = shippingAddressViewModel.IsSameAsBilling ? billingAddressViewModel.City : shippingAddressViewModel.City,
            ShippingAddressPostalCode = shippingAddressViewModel.IsSameAsBilling ? billingAddressViewModel.PostalCode : shippingAddressViewModel.PostalCode,
            ShippingAddressCountryId = shippingAddressViewModel.IsSameAsBilling ? billingCountryId : shippingCountryId,
            ShippingAddressStateId = shippingAddressViewModel.IsSameAsBilling ? billingStateId : shippingStateId,
        };
    }
}
