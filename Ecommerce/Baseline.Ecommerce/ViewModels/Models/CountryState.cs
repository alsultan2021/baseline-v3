namespace Ecommerce.Models;

/// <summary>
/// CountryState model
/// </summary>
public class CountryState
{
    public string CountryCode { get; set; } = string.Empty;
    public string CountryName { get; set; } = string.Empty;
    public string? StateCode { get; set; }
    public string? StateName { get; set; }

    // Additional properties used by chevalroyal views
    public string DisplayName { get; set; } = string.Empty;
    public int CountryId { get; set; }
    public int StateId { get; set; }
}
