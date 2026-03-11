using CMS.ContentEngine;

namespace Baseline.Ecommerce;

/// <summary>
/// Shared utility for extracting product field data.
/// Consolidates duplicated IProductFields cast → reflection fallback patterns
/// from PricingService and PriceCalculationService.
/// </summary>
internal static class ProductFieldHelper
{
    /// <summary>
    /// Gets the price from a product object.
    /// Prefers <see cref="IProductFields"/> interface cast; falls back to reflection.
    /// </summary>
    internal static decimal GetPrice(object product)
    {
        if (product is IProductFields pf)
        {
            return pf.ProductFieldPrice;
        }

        var prop = product.GetType().GetProperty("ProductFieldPrice")
                ?? product.GetType().GetProperty("Price");
        return prop?.GetValue(product) is decimal price ? price : 0m;
    }

    /// <summary>
    /// Gets the tax class GUIDs assigned to a product.
    /// Prefers <see cref="IProductFields"/> interface cast; falls back to reflection.
    /// </summary>
    internal static List<Guid> GetTaxClassGuids(object product)
    {
        if (product is IProductFields pf)
        {
            return pf.ProductFieldTaxClasses?.ToList() ?? [];
        }

        var prop = product.GetType().GetProperty("ProductFieldTaxClasses");
        return (prop?.GetValue(product) as IEnumerable<Guid>)?.ToList() ?? [];
    }

    /// <summary>
    /// Builds a lookup dictionary keyed by <c>ContentItemID</c> from a collection of products.
    /// </summary>
    internal static Dictionary<int, object> BuildLookup(IEnumerable<object> products)
    {
        var lookup = new Dictionary<int, object>();
        foreach (var product in products)
        {
            if (product is IContentItemFieldsSource source)
            {
                lookup[source.SystemFields.ContentItemID] = product;
            }
        }
        return lookup;
    }
}
