namespace Baseline.Core;

/// <summary>
/// Service for generating responsive image HTML with srcset.
/// </summary>
public interface IResponsiveImageService
{
    /// <summary>
    /// Generates a responsive image tag with srcset and sizes attributes.
    /// </summary>
    /// <param name="asset">The content item asset.</param>
    /// <param name="alt">Alt text for the image.</param>
    /// <param name="options">Optional image options.</param>
    /// <returns>HTML string for the responsive image.</returns>
    string GenerateResponsiveImage(object asset, string alt, ResponsiveImageTagOptions? options = null);

    /// <summary>
    /// Generates srcset attribute value for an image.
    /// </summary>
    /// <param name="imageUrl">Base image URL.</param>
    /// <param name="widths">Widths to generate.</param>
    /// <returns>Srcset attribute value.</returns>
    string GenerateSrcSet(string imageUrl, IEnumerable<int>? widths = null);

    /// <summary>
    /// Gets the optimized image URL for a specific width.
    /// </summary>
    /// <param name="imageUrl">Original image URL.</param>
    /// <param name="width">Target width.</param>
    /// <param name="quality">Image quality (1-100).</param>
    /// <param name="format">Image format (webp, jpg, png).</param>
    /// <returns>Optimized image URL.</returns>
    string GetOptimizedImageUrl(string imageUrl, int width, int? quality = null, string? format = null);
}

/// <summary>
/// Options for responsive image tag generation.
/// </summary>
public class ResponsiveImageTagOptions
{
    /// <summary>
    /// Custom widths for srcset generation.
    /// </summary>
    public IEnumerable<int>? Widths { get; set; }

    /// <summary>
    /// Sizes attribute value.
    /// </summary>
    public string? Sizes { get; set; }

    /// <summary>
    /// CSS class names.
    /// </summary>
    public string? CssClass { get; set; }

    /// <summary>
    /// Loading attribute (lazy, eager).
    /// </summary>
    public string? Loading { get; set; }

    /// <summary>
    /// Decoding attribute (async, sync, auto).
    /// </summary>
    public string? Decoding { get; set; } = "async";

    /// <summary>
    /// Fetch priority (high, low, auto).
    /// </summary>
    public string? FetchPriority { get; set; }

    /// <summary>
    /// Image quality (1-100).
    /// </summary>
    public int? Quality { get; set; }

    /// <summary>
    /// Image format (webp, jpg, png).
    /// </summary>
    public string? Format { get; set; }

    /// <summary>
    /// Aspect ratio (e.g., "16/9", "4/3").
    /// </summary>
    public string? AspectRatio { get; set; }

    /// <summary>
    /// Additional HTML attributes.
    /// </summary>
    public Dictionary<string, string>? Attributes { get; set; }
}
