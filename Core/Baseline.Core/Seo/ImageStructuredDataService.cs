using CMS.ContentEngine;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Baseline.Core.Seo;

/// <summary>
/// Interface for generating ImageObject structured data (Schema.org) for SEO optimization.
/// Implements Google's best practices for image structured data.
/// Reference: https://developers.google.com/search/docs/appearance/structured-data/image-license-metadata
/// </summary>
public interface IImageStructuredDataService
{
    /// <summary>
    /// Generates Schema.org ImageObject JSON-LD for a single image.
    /// </summary>
    /// <param name="imageUrl">Full URL to the image.</param>
    /// <param name="altText">Alt text describing the image.</param>
    /// <param name="caption">Optional caption or description.</param>
    /// <param name="width">Image width in pixels.</param>
    /// <param name="height">Image height in pixels.</param>
    /// <param name="copyrightOwner">Copyright holder name.</param>
    /// <param name="creator">Photographer or creator name.</param>
    /// <param name="licenseUrl">URL to license information.</param>
    /// <returns>JSON-LD script tag with ImageObject structured data.</returns>
    string GenerateImageObjectSchema(
        string imageUrl,
        string altText,
        string? caption = null,
        int? width = null,
        int? height = null,
        string? copyrightOwner = null,
        string? creator = null,
        string? licenseUrl = null);

    /// <summary>
    /// Generates Schema.org ImageObject JSON-LD for multiple images (e.g., gallery).
    /// </summary>
    /// <param name="images">Collection of image metadata.</param>
    /// <returns>JSON-LD script tag with array of ImageObject structured data.</returns>
    string GenerateImageGallerySchema(IEnumerable<ImageStructuredMetadata> images);

    /// <summary>
    /// Generates Schema.org ImageObject for ContentItemAsset.
    /// </summary>
    /// <param name="asset">Content item asset.</param>
    /// <param name="altText">Alt text for the image.</param>
    /// <param name="additionalMetadata">Optional additional metadata.</param>
    /// <returns>JSON-LD script tag with ImageObject structured data.</returns>
    string GenerateImageObjectFromAsset(
        ContentItemAsset asset,
        string altText,
        ImageStructuredMetadata? additionalMetadata = null);

    /// <summary>
    /// Creates the ImageObject schema dictionary without wrapping in script tag.
    /// Useful for embedding in larger structured data objects.
    /// </summary>
    /// <param name="metadata">Image metadata.</param>
    /// <returns>Dictionary representing the ImageObject schema.</returns>
    Dictionary<string, object> CreateImageObjectDictionary(ImageStructuredMetadata metadata);
}

/// <summary>
/// Image metadata for structured data generation.
/// </summary>
public sealed record ImageStructuredMetadata
{
    /// <summary>URL of the image.</summary>
    public required string Url { get; init; }

    /// <summary>Alt text describing the image.</summary>
    public required string AltText { get; init; }

    /// <summary>Optional caption for the image.</summary>
    public string? Caption { get; init; }

    /// <summary>Optional description of the image.</summary>
    public string? Description { get; init; }

    /// <summary>Image width in pixels.</summary>
    public int? Width { get; init; }

    /// <summary>Image height in pixels.</summary>
    public int? Height { get; init; }

    /// <summary>Copyright owner name.</summary>
    public string? CopyrightOwner { get; init; }

    /// <summary>Creator/photographer name.</summary>
    public string? Creator { get; init; }

    /// <summary>URL to license information.</summary>
    public string? LicenseUrl { get; init; }

    /// <summary>URL to acquire license.</summary>
    public string? AcquireLicenseUrl { get; init; }

    /// <summary>Date the image was published.</summary>
    public DateTime? DatePublished { get; init; }

    /// <summary>Whether this image is the representative image of the page.</summary>
    public bool? RepresentativeOfPage { get; init; }
}

/// <summary>
/// Service for generating ImageObject structured data (Schema.org) for SEO optimization.
/// Uses the centralized <see cref="IJsonLdGenerator"/> for consistent JSON-LD output.
/// </summary>
internal sealed class ImageStructuredDataService(
    IJsonLdGenerator jsonLdGenerator,
    IHttpContextAccessor httpContextAccessor,
    ILogger<ImageStructuredDataService> logger) : IImageStructuredDataService
{
    public string GenerateImageObjectSchema(
        string imageUrl,
        string altText,
        string? caption = null,
        int? width = null,
        int? height = null,
        string? copyrightOwner = null,
        string? creator = null,
        string? licenseUrl = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(imageUrl) || string.IsNullOrWhiteSpace(altText))
            {
                return string.Empty;
            }

            var metadata = new ImageStructuredMetadata
            {
                Url = imageUrl,
                AltText = altText,
                Caption = caption,
                Width = width,
                Height = height,
                CopyrightOwner = copyrightOwner,
                Creator = creator,
                LicenseUrl = licenseUrl
            };

            return jsonLdGenerator.Generate(CreateImageObjectDictionary(metadata));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating ImageObject schema for {ImageUrl}", imageUrl);
            return string.Empty;
        }
    }

    public string GenerateImageGallerySchema(IEnumerable<ImageStructuredMetadata> images)
    {
        try
        {
            var imageObjects = images
                .Where(img => !string.IsNullOrWhiteSpace(img.Url) && !string.IsNullOrWhiteSpace(img.AltText))
                .Select(CreateImageObjectDictionary)
                .Cast<object>()
                .ToList();

            return imageObjects.Count == 0
                ? string.Empty
                : jsonLdGenerator.Generate(imageObjects);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating image gallery schema");
            return string.Empty;
        }
    }

    public string GenerateImageObjectFromAsset(
        ContentItemAsset asset,
        string altText,
        ImageStructuredMetadata? additionalMetadata = null)
    {
        try
        {
            if (asset?.Url is null)
            {
                return string.Empty;
            }

            var metadata = additionalMetadata is not null
                ? additionalMetadata with
                {
                    Url = asset.Url,
                    AltText = altText,
                    Width = additionalMetadata.Width ?? asset.Metadata?.Width,
                    Height = additionalMetadata.Height ?? asset.Metadata?.Height
                }
                : new ImageStructuredMetadata
                {
                    Url = asset.Url,
                    AltText = altText,
                    Width = asset.Metadata?.Width,
                    Height = asset.Metadata?.Height
                };

            return jsonLdGenerator.Generate(CreateImageObjectDictionary(metadata));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating ImageObject schema from ContentItemAsset");
            return string.Empty;
        }
    }

    public Dictionary<string, object> CreateImageObjectDictionary(ImageStructuredMetadata metadata)
    {
        var schema = new Dictionary<string, object>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "ImageObject",
            ["contentUrl"] = GetAbsoluteUrl(metadata.Url),
            ["name"] = metadata.AltText
        };

        if (!string.IsNullOrWhiteSpace(metadata.Caption))
        {
            schema["caption"] = metadata.Caption;
        }

        if (!string.IsNullOrWhiteSpace(metadata.Description))
        {
            schema["description"] = metadata.Description;
        }

        if (metadata.Width.HasValue && metadata.Height.HasValue)
        {
            schema["width"] = $"{metadata.Width}px";
            schema["height"] = $"{metadata.Height}px";
        }

        if (!string.IsNullOrWhiteSpace(metadata.CopyrightOwner))
        {
            schema["copyrightNotice"] = $"© {DateTime.Now.Year} {metadata.CopyrightOwner}";
            schema["creditText"] = metadata.CopyrightOwner;
        }

        if (!string.IsNullOrWhiteSpace(metadata.Creator))
        {
            schema["creator"] = new Dictionary<string, object>
            {
                ["@type"] = "Person",
                ["name"] = metadata.Creator
            };
        }

        if (!string.IsNullOrWhiteSpace(metadata.LicenseUrl))
        {
            var licenseAbsoluteUrl = GetAbsoluteUrl(metadata.LicenseUrl);
            schema["license"] = licenseAbsoluteUrl;
            schema["acquireLicensePage"] = GetAbsoluteUrl(metadata.AcquireLicenseUrl) ?? licenseAbsoluteUrl;
        }

        if (metadata.DatePublished.HasValue)
        {
            schema["datePublished"] = metadata.DatePublished.Value.ToString("yyyy-MM-dd");
        }

        if (metadata.RepresentativeOfPage.HasValue)
        {
            schema["representativeOfPage"] = metadata.RepresentativeOfPage.Value;
        }

        return schema;
    }

    private string GetAbsoluteUrl(string? relativeUrl)
    {
        if (string.IsNullOrEmpty(relativeUrl) || Uri.IsWellFormedUriString(relativeUrl, UriKind.Absolute))
        {
            return relativeUrl ?? string.Empty;
        }

        var request = httpContextAccessor.HttpContext?.Request;
        if (request == null)
        {
            return relativeUrl;
        }

        if (!relativeUrl.StartsWith('/'))
        {
            relativeUrl = "/" + relativeUrl;
        }

        return $"{request.Scheme}://{request.Host}{relativeUrl}";
    }
}
