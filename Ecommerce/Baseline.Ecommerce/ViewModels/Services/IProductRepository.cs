namespace Ecommerce.Services;

/// <summary>
/// V3 native: IProductRepository interface for product data access.
/// </summary>
public interface IProductRepository
{
    Task<IEnumerable<object>> GetProductsByIdsAsync(IEnumerable<int> productIds, CancellationToken cancellationToken = default);
    Task<Dictionary<int, string>> GetProductPageUrlsAsync(IEnumerable<int> productIds, string? languageName = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the base prices for a collection of products.
    /// </summary>
    /// <param name="productIds">The product content item IDs.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Dictionary mapping product IDs to their base prices.</returns>
    Task<Dictionary<int, decimal>> GetProductPricesAsync(IEnumerable<int> productIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the price for a specific product variant.
    /// </summary>
    /// <param name="productId">The product content item ID.</param>
    /// <param name="variantId">The variant ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The variant price, or null if not found.</returns>
    Task<decimal?> GetVariantPriceAsync(int productId, int variantId, CancellationToken cancellationToken = default);
}

/// <summary>
/// V3 native: IProductRepository generic interface for typed product access.
/// </summary>
public interface IProductRepository<TProduct> : IProductRepository where TProduct : class
{
    Task<TProduct?> GetProductByIdAsync(int productId, CancellationToken cancellationToken = default);
    new Task<IEnumerable<TProduct>> GetProductsByIdsAsync(IEnumerable<int> productIds, CancellationToken cancellationToken = default);
}
