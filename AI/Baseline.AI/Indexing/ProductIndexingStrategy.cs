using Baseline.AI.Indexing;

namespace Baseline.AI;

/// <summary>
/// AI indexing strategy for e-commerce product content types.
/// Enriches embeddings with structured product metadata (price, SKU, availability, categories).
/// </summary>
public class ProductIndexingStrategy : DefaultAIIndexingStrategy
{
    /// <inheritdoc />
    public override string StrategyName => "ECommerce";

    /// <inheritdoc />
    public override string DisplayName => "E-Commerce Product Strategy";

    /// <summary>
    /// Product-specific content fields.
    /// </summary>
    protected override string[] ContentFieldNames =>
    [
        "ProductDescription",
        "Description",
        "ProductShortDescription",
        "Summary",
        "ProductFeatures",
        "Content",
        "Body",
        "RichText"
    ];

    /// <summary>
    /// Product-specific title fields.
    /// </summary>
    protected override string[] TitleFieldNames =>
    [
        "ProductName",
        "Name",
        "Title",
        "SKUName"
    ];

    /// <summary>
    /// Price fields to capture as metadata.
    /// </summary>
    protected virtual string[] PriceFieldNames =>
    [
        "ProductPrice",
        "SKUPrice",
        "Price",
        "ListPrice",
        "SalePrice"
    ];

    /// <summary>
    /// SKU/identifier fields.
    /// </summary>
    protected virtual string[] SkuFieldNames =>
    [
        "SKUNumber",
        "SKU",
        "ProductSKU",
        "ProductCode",
        "ItemNumber"
    ];

    /// <summary>
    /// Availability/stock fields.
    /// </summary>
    protected virtual string[] AvailabilityFieldNames =>
    [
        "SKUAvailableItems",
        "ProductAvailability",
        "InStock",
        "StockQuantity",
        "Available"
    ];

    /// <summary>
    /// Category/taxonomy fields.
    /// </summary>
    protected virtual string[] CategoryFieldNames =>
    [
        "ProductCategory",
        "Category",
        "Categories",
        "Department",
        "ProductType",
        "Brand"
    ];

    /// <summary>
    /// Image fields for metadata.
    /// </summary>
    protected virtual string[] ImageFieldNames =>
    [
        "ProductImage",
        "SKUImagePath",
        "Image",
        "Thumbnail"
    ];

    /// <inheritdoc />
    public override Task<AIExtractResult?> ExtractAsync(
        IAIIndexableItem item,
        CancellationToken cancellationToken = default)
    {
        var content = ExtractContent(item);
        if (string.IsNullOrWhiteSpace(content))
        {
            return Task.FromResult<AIExtractResult?>(null);
        }

        var title = ExtractTitle(item);
        var metadata = BuildProductMetadata(item);

        // Prepend structured product info to embedding text for better retrieval
        var enrichedContent = BuildEnrichedContent(item, title, content);

        var result = new AIExtractResult(
            Content: enrichedContent,
            Title: title,
            Url: item.UrlPath,
            UrlPath: item.UrlPath,
            ContentTypeName: item.ContentTypeName,
            LanguageCode: item.LanguageCode,
            ChannelId: item.ChannelId,
            ChannelName: item.ChannelName,
            ContentItemId: item.ContentItemId,
            ContentItemGuid: item.ContentItemGuid,
            LastModified: DateTime.UtcNow,
            Metadata: metadata
        );

        return Task.FromResult<AIExtractResult?>(result);
    }

    /// <inheritdoc />
    public override IReadOnlyList<AIFieldDefinition> GetFieldDefinitions()
    {
        var definitions = new List<AIFieldDefinition>();

        foreach (var field in TitleFieldNames)
        {
            definitions.Add(new AIFieldDefinition
            {
                FieldName = field,
                IncludeInEmbedding = true,
                StoreAsMetadata = true,
                Weight = 2.0 // Boost product names heavily
            });
        }

        foreach (var field in ContentFieldNames)
        {
            definitions.Add(new AIFieldDefinition
            {
                FieldName = field,
                IncludeInEmbedding = true,
                Weight = 1.0
            });
        }

        foreach (var field in PriceFieldNames)
        {
            definitions.Add(new AIFieldDefinition
            {
                FieldName = field,
                IncludeInEmbedding = false,
                StoreAsMetadata = true,
                Weight = 0
            });
        }

        foreach (var field in SkuFieldNames)
        {
            definitions.Add(new AIFieldDefinition
            {
                FieldName = field,
                IncludeInEmbedding = true,
                StoreAsMetadata = true,
                Weight = 1.5 // SKU queries are common
            });
        }

        foreach (var field in CategoryFieldNames)
        {
            definitions.Add(new AIFieldDefinition
            {
                FieldName = field,
                IncludeInEmbedding = true,
                StoreAsMetadata = true,
                Weight = 1.2
            });
        }

        return definitions;
    }

    /// <summary>
    /// Builds enriched embedding text with structured product info prepended.
    /// </summary>
    private string BuildEnrichedContent(IAIIndexableItem item, string? title, string content)
    {
        var parts = new List<string>();

        if (!string.IsNullOrEmpty(title))
        {
            parts.Add($"Product: {title}");
        }

        var sku = ExtractFirstField(item, SkuFieldNames);
        if (!string.IsNullOrEmpty(sku))
        {
            parts.Add($"SKU: {sku}");
        }

        var price = ExtractFirstField(item, PriceFieldNames);
        if (!string.IsNullOrEmpty(price))
        {
            parts.Add($"Price: {price}");
        }

        var category = ExtractFirstField(item, CategoryFieldNames);
        if (!string.IsNullOrEmpty(category))
        {
            parts.Add($"Category: {category}");
        }

        parts.Add(content);
        return string.Join("\n", parts);
    }

    /// <summary>
    /// Builds product-specific metadata dictionary.
    /// </summary>
    private Dictionary<string, object> BuildProductMetadata(IAIIndexableItem item)
    {
        var metadata = new Dictionary<string, object>
        {
            ["contentItemId"] = item.ContentItemId,
            ["contentItemGuid"] = item.ContentItemGuid.ToString(),
            ["contentTypeName"] = item.ContentTypeName,
            ["languageCode"] = item.LanguageCode,
            ["isProduct"] = true
        };

        var title = ExtractTitle(item);
        if (!string.IsNullOrEmpty(title))
        {
            metadata["title"] = title;
        }

        SetMetadataFromFields(item, metadata, "sku", SkuFieldNames);
        SetMetadataFromFields(item, metadata, "price", PriceFieldNames);
        SetMetadataFromFields(item, metadata, "availability", AvailabilityFieldNames);
        SetMetadataFromFields(item, metadata, "category", CategoryFieldNames);
        SetMetadataFromFields(item, metadata, "image", ImageFieldNames);

        return metadata;
    }

    /// <summary>
    /// Extracts the first non-empty value from a set of field names.
    /// </summary>
    private static string? ExtractFirstField(IAIIndexableItem item, string[] fieldNames)
    {
        foreach (var fieldName in fieldNames)
        {
            var value = item.GetFieldValue(fieldName);
            if (value is not null)
            {
                var str = value.ToString();
                if (!string.IsNullOrWhiteSpace(str))
                {
                    return str;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Sets metadata from the first matching field.
    /// </summary>
    private static void SetMetadataFromFields(
        IAIIndexableItem item,
        Dictionary<string, object> metadata,
        string metadataKey,
        string[] fieldNames)
    {
        foreach (var fieldName in fieldNames)
        {
            var value = item.GetFieldValue(fieldName);
            if (value is not null)
            {
                metadata[metadataKey] = value;
                return;
            }
        }
    }
}
