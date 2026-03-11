namespace Ecommerce.Services;

/// <summary>
/// ICountryStateRepository - provides country/state data.
/// </summary>
public interface ICountryStateRepository
{
    Task<IEnumerable<Ecommerce.Models.CountryState>> GetCountriesAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Ecommerce.Models.CountryState>> GetStatesAsync(int countryId, CancellationToken cancellationToken = default);
}
