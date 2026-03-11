namespace Ecommerce.Models;

/// <summary>
/// OrderAddress for shipping address in price calculation.
/// </summary>
public class OrderAddress
{
    public string? Street { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public int? CountryId { get; set; }
    public int? StateId { get; set; }
}
