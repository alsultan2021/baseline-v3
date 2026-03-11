namespace Baseline.Ecommerce.Interfaces;

/// <summary>
/// Provides product name formatting with optional variant information.
/// </summary>
public interface IProductNameProvider
{
    /// <summary>
    /// Gets the formatted product name, including variant information if applicable.
    /// </summary>
    /// <typeparam name="TProduct">The type of product.</typeparam>
    /// <param name="product">The product to get the name for.</param>
    /// <param name="variantId">Optional variant ID to include variant-specific naming.</param>
    /// <returns>The formatted product name.</returns>
    string GetProductName<TProduct>(TProduct product, string? variantId = null);
}
