using Baseline.Ecommerce.Interfaces;

namespace Baseline.Ecommerce.Services;

/// <summary>
/// Default implementation that aggregates all registered <see cref="IProductTypeParametersExtractor"/> implementations
/// to extract parameters from products.
/// </summary>
/// <param name="extractors">The registered product type parameter extractors.</param>
public sealed class ProductParametersExtractor(IEnumerable<IProductTypeParametersExtractor> extractors) : IProductParametersExtractor
{
    /// <inheritdoc />
    public async Task<IDictionary<string, string>> ExtractParametersAsync<TProduct>(TProduct product, string languageName, CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, string>();

        foreach (var extractor in extractors)
        {
            await extractor.ExtractParameterAsync(parameters, product, languageName, cancellationToken);
        }

        return parameters;
    }
}
