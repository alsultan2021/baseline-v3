namespace Baseline.Ecommerce.Models;

/// <summary>
/// Data transfer object for customer information in the checkout process.
/// </summary>
public sealed record CustomerDto
{
    /// <summary>
    /// Customer's first name.
    /// </summary>
    public required string FirstName { get; set; }

    /// <summary>
    /// Customer's last name.
    /// </summary>
    public required string LastName { get; set; }

    /// <summary>
    /// Customer's email address.
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// Customer's phone number.
    /// </summary>
    public required string PhoneNumber { get; set; }

    /// <summary>
    /// Customer's company name (optional).
    /// </summary>
    public string? Company { get; set; }

    /// <summary>
    /// Billing address line 1.
    /// </summary>
    public required string BillingAddressLine1 { get; set; }

    /// <summary>
    /// Billing address line 2 (optional).
    /// </summary>
    public string? BillingAddressLine2 { get; set; }

    /// <summary>
    /// Billing address city.
    /// </summary>
    public required string BillingAddressCity { get; set; }

    /// <summary>
    /// Billing address postal/zip code.
    /// </summary>
    public required string BillingAddressPostalCode { get; set; }

    /// <summary>
    /// Billing address country ID.
    /// </summary>
    public int BillingAddressCountryId { get; set; }

    /// <summary>
    /// Billing address state/province ID.
    /// </summary>
    public int BillingAddressStateId { get; set; }

    /// <summary>
    /// Shipping address line 1 (optional, if different from billing).
    /// </summary>
    public string? ShippingAddressLine1 { get; set; }

    /// <summary>
    /// Shipping address line 2 (optional).
    /// </summary>
    public string? ShippingAddressLine2 { get; set; }

    /// <summary>
    /// Shipping address city (optional).
    /// </summary>
    public string? ShippingAddressCity { get; set; }

    /// <summary>
    /// Shipping address postal/zip code (optional).
    /// </summary>
    public string? ShippingAddressPostalCode { get; set; }

    /// <summary>
    /// Shipping address country ID.
    /// </summary>
    public int ShippingAddressCountryId { get; set; }

    /// <summary>
    /// Shipping address state/province ID.
    /// </summary>
    public int ShippingAddressStateId { get; set; }

    /// <summary>
    /// Gets the customer's full name.
    /// </summary>
    public string FullName => $"{FirstName} {LastName}".Trim();

    /// <summary>
    /// Indicates whether shipping address is different from billing address.
    /// </summary>
    public bool HasSeparateShippingAddress =>
        !string.IsNullOrEmpty(ShippingAddressLine1) &&
        (ShippingAddressLine1 != BillingAddressLine1 ||
         ShippingAddressCity != BillingAddressCity ||
         ShippingAddressPostalCode != BillingAddressPostalCode);
}
