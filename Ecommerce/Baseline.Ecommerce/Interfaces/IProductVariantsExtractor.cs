namespace Baseline.Ecommerce.Interfaces;

/// <summary>
/// Extractor of product variants based on the product type.
/// Site implementations can add extractors for specific product types.
/// </summary>
public interface IProductTypeVariantsExtractor
{
    /// <summary>
    /// Extract product-specific variants value of a product based on its type.
    /// </summary>
    /// <typeparam name="T">Type of the product.</typeparam>
    /// <param name="product">Product to get variants from.</param>
    /// <returns>Dictionary of variant ID to variant display value.</returns>
    IDictionary<int, string> ExtractVariantsValue<T>(T product);

    /// <summary>
    /// Extract product-specific SKU code of variants of a product based on its type.
    /// </summary>
    /// <typeparam name="T">Type of the product.</typeparam>
    /// <param name="product">Product to get SKU code variants from.</param>
    /// <returns>Dictionary of variant ID to variant SKU code.</returns>
    IDictionary<int, string> ExtractVariantsSKUCode<T>(T product);
}

/// <summary>
/// Aggregates all registered <see cref="IProductTypeVariantsExtractor"/> implementations
/// to extract variant information from products.
/// </summary>
public interface IProductVariantsExtractor
{
    /// <summary>
    /// Extract product variants and return dictionary of variant ID to display value.
    /// </summary>
    /// <typeparam name="TProduct">The type of product.</typeparam>
    /// <param name="product">The product to extract variants from.</param>
    /// <returns>Dictionary of variant ID to variant display value.</returns>
    IDictionary<int, string> ExtractVariantsValue<TProduct>(TProduct product);

    /// <summary>
    /// Extract product variants SKU codes and return dictionary of variant ID to SKU code.
    /// </summary>
    /// <typeparam name="TProduct">The type of product.</typeparam>
    /// <param name="product">The product to extract variant SKU codes from.</param>
    /// <returns>Dictionary of variant ID to variant SKU code, or null if no variants.</returns>
    IDictionary<int, string>? ExtractVariantsSKUCode<TProduct>(TProduct product);
}
