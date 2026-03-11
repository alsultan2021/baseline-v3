using CMS.Commerce;
using Microsoft.Extensions.Logging;

namespace Baseline.Ecommerce;

/// <summary>
/// Abstract base implementation of <see cref="IProductDataRetriever{TProductIdentifier, TProductData}"/>.
/// Projects must provide a concrete implementation that knows how to retrieve product data from their catalog.
/// </summary>
/// <remarks>
/// Per Kentico Commerce documentation, the product data retriever is data store agnostic - you can retrieve
/// product data from Xperience's content hub, external databases, third-party APIs, or custom data stores.
/// </remarks>
/// <typeparam name="TProductIdentifier">The product identifier type.</typeparam>
/// <typeparam name="TProductData">The product data type.</typeparam>
public abstract class ProductDataRetrieverBase<TProductIdentifier, TProductData>(
    ILogger logger) : IProductDataRetriever<TProductIdentifier, TProductData>
    where TProductIdentifier : ProductIdentifier
    where TProductData : ProductData
{
    /// <summary>
    /// Gets the logger instance.
    /// </summary>
    protected ILogger Logger => logger;

    /// <inheritdoc/>
    public async Task<IReadOnlyDictionary<TProductIdentifier, TProductData>> Get(
        IEnumerable<TProductIdentifier> productIdentifiers,
        string languageName,
        CancellationToken cancellationToken = default)
    {
        var identifiers = productIdentifiers.ToList();

        if (identifiers.Count == 0)
        {
            return new Dictionary<TProductIdentifier, TProductData>();
        }

        Logger.LogDebug("Retrieving product data for {Count} products, language: {Language}",
            identifiers.Count, languageName);

        var result = await RetrieveProductDataAsync(identifiers, languageName, cancellationToken);

        Logger.LogDebug("Retrieved product data for {Count} of {Total} products",
            result.Count, identifiers.Count);

        return result;
    }

    /// <summary>
    /// Retrieves product data for the given identifiers.
    /// Implement this method to query your product catalog.
    /// </summary>
    /// <param name="productIdentifiers">The product identifiers to retrieve.</param>
    /// <param name="languageName">The language name for localized data.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A dictionary mapping product identifiers to their data.</returns>
    protected abstract Task<IReadOnlyDictionary<TProductIdentifier, TProductData>> RetrieveProductDataAsync(
        IReadOnlyList<TProductIdentifier> productIdentifiers,
        string languageName,
        CancellationToken cancellationToken);
}

/// <summary>
/// No-op implementation of <see cref="IProductDataRetriever{TProductIdentifier, TProductData}"/>
/// for development/testing when a real product catalog isn't available.
/// Returns zero prices for all products.
/// </summary>
public class NoOpProductDataRetriever(
    ILogger<NoOpProductDataRetriever> logger) : IProductDataRetriever<ProductIdentifier, ProductData>
{
    /// <inheritdoc/>
    public Task<IReadOnlyDictionary<ProductIdentifier, ProductData>> Get(
        IEnumerable<ProductIdentifier> productIdentifiers,
        string languageName,
        CancellationToken cancellationToken = default)
    {
        var identifiers = productIdentifiers.ToList();

        logger.LogWarning(
            "NoOpProductDataRetriever: Returning zero prices for {Count} products. " +
            "Implement a real IProductDataRetriever for production use.",
            identifiers.Count);

        // Return zero-priced product data for all identifiers
        var result = identifiers.ToDictionary(
            id => id,
            _ => new ProductData { UnitPrice = 0m });

        return Task.FromResult<IReadOnlyDictionary<ProductIdentifier, ProductData>>(result);
    }
}

/// <summary>
/// Extended product identifier that includes variant information.
/// </summary>
public record ExtendedProductIdentifier : ProductIdentifier
{
    /// <summary>
    /// The variant ID for product variants.
    /// </summary>
    public int? VariantId { get; init; }
}

/// <summary>
/// Extended product data with additional fields for advanced pricing scenarios.
/// </summary>
public record ExtendedProductData : ProductData
{
    /// <summary>
    /// Product weight for shipping calculations.
    /// </summary>
    public decimal Weight { get; init; }

    /// <summary>
    /// Tax category for tax calculations.
    /// </summary>
    public string TaxCategory { get; init; } = "Standard";

    /// <summary>
    /// Product category for category-based discounts.
    /// </summary>
    public string ProductCategory { get; init; } = "General";

    /// <summary>
    /// Whether this is a digital/downloadable product.
    /// </summary>
    public bool IsDigital { get; init; }

    /// <summary>
    /// Whether this product is tax exempt.
    /// </summary>
    public bool IsTaxExempt { get; init; }
}
