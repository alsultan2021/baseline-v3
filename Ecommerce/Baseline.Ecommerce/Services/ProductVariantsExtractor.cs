using Baseline.Ecommerce.Interfaces;

namespace Baseline.Ecommerce.Services;

/// <summary>
/// Default implementation that aggregates all registered <see cref="IProductTypeVariantsExtractor"/> implementations
/// to extract variant information from products.
/// </summary>
/// <param name="extractors">The registered product type variant extractors.</param>
public sealed class ProductVariantsExtractor(IEnumerable<IProductTypeVariantsExtractor> extractors) : IProductVariantsExtractor
{
    /// <inheritdoc />
    public IDictionary<int, string> ExtractVariantsValue<TProduct>(TProduct product)
    {
        var result = new Dictionary<int, string>();

        foreach (var extractor in extractors)
        {
            var variants = extractor.ExtractVariantsValue(product);
            if (variants != null)
            {
                foreach (var (key, value) in variants)
                {
                    result.TryAdd(key, value);
                }
            }
        }

        return result;
    }

    /// <inheritdoc />
    public IDictionary<int, string>? ExtractVariantsSKUCode<TProduct>(TProduct product)
    {
        var result = new Dictionary<int, string>();

        foreach (var extractor in extractors)
        {
            var variants = extractor.ExtractVariantsSKUCode(product);
            if (variants != null)
            {
                foreach (var (key, value) in variants)
                {
                    result.TryAdd(key, value);
                }
            }
        }

        return result.Count > 0 ? result : null;
    }
}
