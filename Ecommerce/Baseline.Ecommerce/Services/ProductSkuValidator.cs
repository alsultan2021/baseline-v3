using Baseline.Ecommerce.Interfaces;
using CMS.ContentEngine;

namespace Baseline.Ecommerce.Services;

/// <summary>
/// Default implementation for validating product SKU codes against existing content items.
/// Ensures SKU codes are unique across published and draft versions.
/// </summary>
/// <param name="executor">The content query executor.</param>
public sealed class ProductSkuValidator(IContentQueryExecutor executor) : IProductSkuValidator
{
    /// <inheritdoc />
    public async Task<int?> GetCollidingContentItem(string skuCode, int? contentItemId = null)
    {
        var queryBuilder = new ContentItemQueryBuilder()
            .ForContentTypes(ct => ct.OfReusableSchema("IProductSKU"))
            .Parameters(p => p.Where(w => w.WhereEquals("ProductSKUCode", skuCode)));

        if (contentItemId.HasValue)
        {
            queryBuilder.Parameters(p =>
                p.Where(w => w.WhereNotEquals(nameof(IContentItemFieldsSource.SystemFields.ContentItemID), contentItemId.Value)));
        }

        // Check published versions
        var publishedDuplicates = await executor.GetResult<int?>(
            queryBuilder,
            rowData => rowData.ContentItemID,
            new ContentQueryExecutionOptions { ForPreview = false });

        if (publishedDuplicates.FirstOrDefault() is int publishedId)
        {
            return publishedId;
        }

        // Check draft versions
        queryBuilder.Parameters(p =>
            p.Where(w => w.WhereIn(
                nameof(IContentItemFieldsSource.SystemFields.ContentItemCommonDataVersionStatus),
                [(int)VersionStatus.InitialDraft, (int)VersionStatus.Draft])));

        var draftDuplicates = await executor.GetResult<int?>(
            queryBuilder,
            rowData => rowData.ContentItemID,
            new ContentQueryExecutionOptions { ForPreview = true });

        return draftDuplicates.FirstOrDefault();
    }
}
