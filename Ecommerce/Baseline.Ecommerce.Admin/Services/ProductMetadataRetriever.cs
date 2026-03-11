using CMS.ContentEngine;
using Baseline.Ecommerce.Models;

namespace Baseline.Ecommerce.Admin.Services;

/// <summary>
/// Service responsible for retrieving product metadata (display names, etc.) from content items.
/// This is essential for displaying meaningful product information in the admin interface.
/// </summary>
public interface IProductMetadataRetriever
{
    /// <summary>
    /// Retrieves the product display name for a given product stock record.
    /// </summary>
    /// <param name="productStockInfo">The product stock record.</param>
    /// <returns>The product display name, or null if not found.</returns>
    Task<string?> GetProductDisplayNameAsync(ProductStockInfo productStockInfo);

    /// <summary>
    /// Retrieves the product display name for a given content item GUID.
    /// </summary>
    /// <param name="productGuid">The product content item GUID.</param>
    /// <returns>The product display name, or null if not found.</returns>
    Task<string?> GetProductDisplayNameAsync(Guid productGuid);
}

/// <inheritdoc />
public sealed class ProductMetadataRetriever : IProductMetadataRetriever
{
    private readonly IContentQueryExecutor contentQueryExecutor;
    private readonly IDefaultContentLanguageRetriever defaultContentLanguageRetriever;

    public ProductMetadataRetriever(
        IContentQueryExecutor contentQueryExecutor,
        IDefaultContentLanguageRetriever defaultContentLanguageRetriever)
    {
        this.contentQueryExecutor = contentQueryExecutor;
        this.defaultContentLanguageRetriever = defaultContentLanguageRetriever;
    }

    /// <inheritdoc />
    public async Task<string?> GetProductDisplayNameAsync(ProductStockInfo productStockInfo)
    {
        var productGuid = productStockInfo.GetProductGuid();
        if (productGuid == null || productGuid == Guid.Empty)
        {
            return null;
        }

        return await GetProductDisplayNameAsync(productGuid.Value);
    }

    /// <inheritdoc />
    public async Task<string?> GetProductDisplayNameAsync(Guid productGuid)
    {
        if (productGuid == Guid.Empty)
        {
            return null;
        }

        try
        {
            // Uses the default language to ensure consistent metadata retrieval
            var defaultContentLanguage = await defaultContentLanguageRetriever.GetAsync();

            // Query for the content item by GUID to get its display name
            var builder = new ContentItemQueryBuilder()
                .ForContentTypes(q => q.WithContentTypeFields())
                .InLanguage(defaultContentLanguage.ContentLanguageName)
                .Parameters(p => p.Where(w => w.WhereEquals("ContentItemGUID", productGuid)));

            var results = await contentQueryExecutor.GetMappedResult<IContentItemFieldsSource>(
                builder,
                new ContentQueryExecutionOptions { ForPreview = true, IncludeSecuredItems = true });

            var contentItem = results.FirstOrDefault();
            return contentItem?.SystemFields.ContentItemName;
        }
        catch
        {
            // Return null if metadata retrieval fails (product may be deleted, etc.)
            return null;
        }
    }
}
