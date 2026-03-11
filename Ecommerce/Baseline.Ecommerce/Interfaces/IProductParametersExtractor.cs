namespace Baseline.Ecommerce.Interfaces;

/// <summary>
/// Extractor of product parameters based on the product type.
/// Site implementations can add extractors for specific product types.
/// </summary>
public interface IProductTypeParametersExtractor
{
    /// <summary>
    /// Extract product-specific parameters of a product based on its type and update the parameters dictionary.
    /// </summary>
    /// <typeparam name="T">Type of the product.</typeparam>
    /// <param name="parameters">Dictionary containing parameters of the product that will be updated.</param>
    /// <param name="product">Product to get parameters from.</param>
    /// <param name="languageName">Language name to use.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ExtractParameterAsync<T>(IDictionary<string, string> parameters, T product, string languageName, CancellationToken cancellationToken);
}

/// <summary>
/// Aggregates all registered <see cref="IProductTypeParametersExtractor"/> implementations
/// to extract parameters from products.
/// </summary>
public interface IProductParametersExtractor
{
    /// <summary>
    /// Extract product parameters and return dictionary of parameters.
    /// </summary>
    /// <typeparam name="TProduct">The type of product.</typeparam>
    /// <param name="product">Product to process.</param>
    /// <param name="languageName">Language name used.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dictionary containing product parameters.</returns>
    Task<IDictionary<string, string>> ExtractParametersAsync<TProduct>(TProduct product, string languageName, CancellationToken cancellationToken);
}
