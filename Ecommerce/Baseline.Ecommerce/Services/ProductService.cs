using Ecommerce.Services;
using Microsoft.Extensions.Logging;

namespace Baseline.Ecommerce;

/// <summary>
/// Default implementation of IProductService using IProductRepository for content queries.
/// This provides a bridge between the Baseline domain model and site-specific content types.
/// </summary>
public class ProductService(
    IProductRepository productRepository,
    ILogger<ProductService> logger) : IProductService
{
    /// <inheritdoc/>
    public async Task<Product?> GetProductAsync(int productId)
    {
        logger.LogDebug("Getting product: {ProductId}", productId);

        try
        {
            var products = await productRepository.GetProductsByIdsAsync([productId]);
            var product = products.FirstOrDefault();

            if (product == null)
            {
                return null;
            }

            return MapToProduct(product);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get product: {ProductId}", productId);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<Product?> GetProductBySkuAsync(string sku)
    {
        logger.LogDebug("Getting product by SKU: {Sku}", sku);

        // Note: SKU-based lookup requires extending IProductRepository with a new method
        // For now, this is not directly supported by the base interface
        logger.LogWarning("GetProductBySkuAsync requires implementation of IProductRepository.GetProductBySkuAsync");
        await Task.CompletedTask;
        return null;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(Guid categoryId)
    {
        logger.LogDebug("Getting products by category: {CategoryId}", categoryId);

        // Note: Category-based lookup requires extending IProductRepository or a separate category service
        // The current IProductRepository interface doesn't support category queries
        logger.LogWarning("GetProductsByCategoryAsync requires implementation of category-based product queries");
        await Task.CompletedTask;
        return [];
    }

    /// <inheritdoc/>
    public async Task<PagedResult<Product>> SearchProductsAsync(ProductSearchRequest request)
    {
        logger.LogDebug("Searching products: Query={Query}, Page={Page}", request.Query, request.Page);

        // Note: Full-text search requires Kentico Search integration or custom implementation
        // This is a placeholder that returns empty results
        logger.LogWarning("SearchProductsAsync requires implementation of product search functionality");
        await Task.CompletedTask;

        return new PagedResult<Product>
        {
            Items = [],
            TotalCount = 0,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    /// <inheritdoc/>
    public async Task<ProductAvailability> GetAvailabilityAsync(int productId)
    {
        logger.LogDebug("Getting availability: {ProductId}", productId);

        try
        {
            var products = await productRepository.GetProductsByIdsAsync([productId]);
            var product = products.FirstOrDefault();

            if (product == null)
            {
                return new ProductAvailability
                {
                    InStock = false,
                    StockQuantity = 0,
                    AvailabilityText = "Product not found"
                };
            }

            // Default availability - products are assumed to be in stock
            // Stock management requires additional content type fields
            return new ProductAvailability
            {
                InStock = true,
                StockQuantity = null,
                AllowBackorder = false,
                AvailabilityText = "In Stock"
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get availability: {ProductId}", productId);
            return new ProductAvailability
            {
                InStock = false,
                AvailabilityText = "Unable to check availability"
            };
        }
    }

    /// <inheritdoc/>
    public async Task<ProductPrice> GetPriceAsync(int productId, int quantity = 1)
    {
        logger.LogDebug("Getting price: ProductId={ProductId}, Quantity={Quantity}", productId, quantity);

        try
        {
            var products = await productRepository.GetProductsByIdsAsync([productId]);
            var product = products.FirstOrDefault();

            if (product == null)
            {
                return new ProductPrice
                {
                    Price = Money.Zero(),
                    SalePrice = null
                };
            }

            var price = GetProductPrice(product);

            return new ProductPrice
            {
                Price = new Money { Amount = price * quantity, Currency = "USD" },
                SalePrice = null
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get price: {ProductId}", productId);
            return new ProductPrice
            {
                Price = Money.Zero(),
                SalePrice = null
            };
        }
    }

    #region Private Helpers

    private static Product MapToProduct(object productData)
    {
        var contentItemId = GetContentItemId(productData);
        var name = GetProductName(productData);
        var price = GetProductPrice(productData);
        var sku = GetProductSku(productData);
        var imageUrl = GetProductImageUrl(productData);
        var description = GetProductDescription(productData);

        return new Product
        {
            Id = contentItemId,
            Name = name,
            Sku = sku,
            Description = description,
            Price = new Money { Amount = price, Currency = "USD" },
            ImageUrl = imageUrl,
            Availability = new ProductAvailability
            {
                InStock = true,
                AvailabilityText = "In Stock"
            }
        };
    }

    private static int GetContentItemId(object product)
    {
        if (product.GetType().GetProperty("SystemFields")?.GetValue(product) is { } systemFields)
        {
            if (systemFields.GetType().GetProperty("ContentItemID")?.GetValue(systemFields) is int id)
            {
                return id;
            }
        }
        return 0;
    }

    private static string GetProductName(object product)
    {
        var prop = product.GetType().GetProperty("ProductFieldName") ?? product.GetType().GetProperty("Name");
        return prop?.GetValue(product)?.ToString() ?? "Unknown Product";
    }

    private static decimal GetProductPrice(object product)
    {
        var prop = product.GetType().GetProperty("ProductFieldPrice") ?? product.GetType().GetProperty("Price");
        return prop?.GetValue(product) is decimal price ? price : 0m;
    }

    private static string? GetProductSku(object product)
    {
        var prop = product.GetType().GetProperty("ProductSKUCode") ?? product.GetType().GetProperty("Sku");
        return prop?.GetValue(product)?.ToString();
    }

    private static string? GetProductImageUrl(object product)
    {
        var prop = product.GetType().GetProperty("ProductFieldImage");
        if (prop?.GetValue(product) is IEnumerable<object> images)
        {
            var firstImage = images.FirstOrDefault();
            if (firstImage != null)
            {
                var assetProp = firstImage.GetType().GetProperty("ImageAsset");
                var asset = assetProp?.GetValue(firstImage);
                var urlProp = asset?.GetType().GetProperty("Url");
                return urlProp?.GetValue(asset)?.ToString();
            }
        }
        return null;
    }

    private static string? GetProductDescription(object product)
    {
        var prop = product.GetType().GetProperty("ProductFieldDescription") ?? product.GetType().GetProperty("Description");
        return prop?.GetValue(product)?.ToString();
    }

    #endregion
}
