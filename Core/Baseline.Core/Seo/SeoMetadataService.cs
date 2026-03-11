using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using CMS.ContentEngine;
using CMS.Websites;
using CMS.Websites.Routing;

namespace Baseline.Core.Seo;

using Baseline.Core;

/// <summary>
/// Service that automatically extracts SEO metadata from content items
/// using conventions and attributes.
/// </summary>
public interface ISeoMetadataService
{
    /// <summary>
    /// Extracts SEO metadata from a content item using conventions.
    /// </summary>
    Task<SeoMetadata> GetMetadataAsync<TContent>(TContent content, IWebsiteChannelContext channelContext)
        where TContent : class;

    /// <summary>
    /// Extracts SEO metadata from a web page using conventions.
    /// </summary>
    Task<SeoMetadata> GetMetadataAsync<TWebPage>(TWebPage webPage, string language)
        where TWebPage : class, IWebPageFieldsSource;

    /// <summary>
    /// Generates JSON-LD structured data for a content item.
    /// </summary>
    Task<string?> GenerateStructuredDataAsync<TContent>(TContent content, string schemaType, string pageUrl)
        where TContent : class;
}

/// <summary>
/// Default implementation of ISeoMetadataService using conventions and reflection.
/// 
/// Conventions:
/// 1. If content implements ISeoMetadataProvider, use those properties
/// 2. If content implements IBaseMetadata schema, use MetaData_* fields
/// 3. Fall back to common field name conventions (Title, Description, etc.)
/// </summary>
public class SeoMetadataService : ISeoMetadataService
{
    private readonly IWebsiteChannelContext _channelContext;
    private static readonly ConcurrentDictionary<(Type, string), PropertyInfo?> _propertyCache = new();
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = null,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public SeoMetadataService(IWebsiteChannelContext channelContext)
    {
        _channelContext = channelContext;
    }

    public async Task<SeoMetadata> GetMetadataAsync<TContent>(TContent content, IWebsiteChannelContext channelContext)
        where TContent : class
    {
        var metadata = new SeoMetadata();

        // Try ISeoMetadataProvider first
        if (content is ISeoMetadataProvider seoProvider)
        {
            return await Task.FromResult(new SeoMetadata
            {
                Title = seoProvider.SeoTitle ?? "",
                Description = seoProvider.SeoDescription ?? "",
                OgTitle = seoProvider.SeoTitle,
                OgDescription = seoProvider.SeoDescription,
                OgImage = seoProvider.SeoOgImage,
                CanonicalUrl = seoProvider.SeoCanonicalUrl,
                Robots = seoProvider.SeoRobots,
                IncludeInSitemap = seoProvider.SeoIncludeInSitemap
            });
        }

        // Try IBaseMetadata convention (MetaData_Title, MetaData_Description, etc.)
        var type = content.GetType();
        var result = new SeoMetadata();

        // Check for MetaData_* properties from IBaseMetadata schema
        result = result with
        {
            Title = GetPropertyValue<string>(content, "MetaData_Title") ?? GetPropertyValue<string>(content, "Title") ?? "",
            Description = GetPropertyValue<string>(content, "MetaData_Description") ?? GetPropertyValue<string>(content, "Description") ?? "",
            OgImage = GetOgImageUrl(content) ?? GetPropertyValue<string>(content, "OgImage"),
            Robots = GetPropertyValue<string>(content, "MetaData_Robots"),
            IncludeInSitemap = GetPropertyValue<bool?>(content, "MetaData_ShowInSitemap") ?? true
        };

        // Set OG defaults from main fields if not specified
        if (string.IsNullOrEmpty(result.OgTitle))
            result = result with { OgTitle = result.Title };
        if (string.IsNullOrEmpty(result.OgDescription))
            result = result with { OgDescription = result.Description };

        return await Task.FromResult(result);
    }

    public async Task<SeoMetadata> GetMetadataAsync<TWebPage>(TWebPage webPage, string language)
        where TWebPage : class, IWebPageFieldsSource
    {
        return await GetMetadataAsync(webPage, _channelContext);
    }

    public async Task<string?> GenerateStructuredDataAsync<TContent>(TContent content, string schemaType, string pageUrl)
        where TContent : class
    {
        var metadata = await GetMetadataAsync(content, _channelContext);

        // Build structured data based on schema type
        var structuredData = schemaType switch
        {
            "Article" => GenerateArticleSchema(metadata, pageUrl),
            "Product" => GenerateProductSchema(content, metadata, pageUrl),
            "Organization" => GenerateOrganizationSchema(metadata, pageUrl),
            "BreadcrumbList" => null, // Handled separately
            _ => GenerateWebPageSchema(metadata, pageUrl)
        };

        return structuredData;
    }

    private static T? GetPropertyValue<T>(object obj, string propertyName)
    {
        var prop = _propertyCache.GetOrAdd((obj.GetType(), propertyName), static key =>
            key.Item1.GetProperty(key.Item2));
        if (prop == null) return default;

        var value = prop.GetValue(obj);
        if (value is T typedValue) return typedValue;

        return default;
    }

    /// <summary>
    /// Extracts OG image URL from content's MetaData_OGImage collection (IEnumerable&lt;IGenericHasImage&gt;).
    /// </summary>
    private static string? GetOgImageUrl(object content)
    {
        var prop = _propertyCache.GetOrAdd((content.GetType(), "MetaData_OGImage"), static key =>
            key.Item1.GetProperty(key.Item2));
        if (prop == null) return null;

        var value = prop.GetValue(content);
        if (value is not IEnumerable<IGenericHasImage> ogImages)
            return null;

        return OgImageHelper.ExtractImageUrl(ogImages.FirstOrDefault());
    }

    private string GenerateWebPageSchema(SeoMetadata metadata, string pageUrl)
    {
        var schema = new Dictionary<string, object>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "WebPage",
            ["name"] = metadata.Title,
            ["description"] = metadata.Description,
            ["url"] = pageUrl
        };

        if (!string.IsNullOrEmpty(metadata.OgImage))
            schema["image"] = metadata.OgImage;

        return JsonSerializer.Serialize(schema, _jsonOptions);
    }

    private string GenerateArticleSchema(SeoMetadata metadata, string pageUrl)
    {
        var schema = new Dictionary<string, object>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "Article",
            ["headline"] = metadata.Title,
            ["description"] = metadata.Description,
            ["url"] = pageUrl
        };

        if (!string.IsNullOrEmpty(metadata.OgImage))
            schema["image"] = metadata.OgImage;
        if (!string.IsNullOrEmpty(metadata.Author))
            schema["author"] = new Dictionary<string, object> { ["@type"] = "Person", ["name"] = metadata.Author };
        if (metadata.PublishedDate.HasValue)
            schema["datePublished"] = metadata.PublishedDate.Value.ToString("O");
        if (metadata.ModifiedDate.HasValue)
            schema["dateModified"] = metadata.ModifiedDate.Value.ToString("O");

        return JsonSerializer.Serialize(schema, _jsonOptions);
    }

    private string GenerateProductSchema<TContent>(TContent content, SeoMetadata metadata, string pageUrl)
        where TContent : class
    {
        var price = GetPropertyValue<decimal?>(content, "Price") ?? GetPropertyValue<decimal?>(content, "ProductPrice");
        var currency = GetPropertyValue<string>(content, "Currency") ?? "USD";

        var schema = new Dictionary<string, object>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "Product",
            ["name"] = metadata.Title,
            ["description"] = metadata.Description,
            ["url"] = pageUrl
        };

        if (!string.IsNullOrEmpty(metadata.OgImage))
            schema["image"] = metadata.OgImage;
        if (price.HasValue)
            schema["offers"] = new Dictionary<string, object> { ["@type"] = "Offer", ["price"] = price.Value.ToString(), ["priceCurrency"] = currency };

        return JsonSerializer.Serialize(schema, _jsonOptions);
    }

    private string GenerateOrganizationSchema(SeoMetadata metadata, string pageUrl)
    {
        var schema = new Dictionary<string, object>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "Organization",
            ["name"] = metadata.Title,
            ["description"] = metadata.Description,
            ["url"] = pageUrl
        };

        if (!string.IsNullOrEmpty(metadata.OgImage))
            schema["logo"] = metadata.OgImage;

        return JsonSerializer.Serialize(schema, _jsonOptions);
    }
}
