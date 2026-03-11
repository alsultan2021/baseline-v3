using CMS.ContentEngine;

namespace Baseline.Core;

/// <summary>
/// Default implementation of <see cref="IResponsiveImageService"/>.
/// </summary>
internal sealed class ResponsiveImageService(ResponsiveImageOptions options) : IResponsiveImageService
{
    public string GenerateResponsiveImage(object asset, string alt, ResponsiveImageTagOptions? tagOptions = null)
    {
        // Extract URL from asset (ContentItemAsset or string URL)
        var imageUrl = ExtractImageUrl(asset);
        if (string.IsNullOrEmpty(imageUrl))
        {
            return string.Empty;
        }

        var widths = tagOptions?.Widths ?? options.DefaultWidths;
        var srcset = GenerateSrcSet(imageUrl, widths);
        var sizes = tagOptions?.Sizes ?? options.DefaultSizes;
        var loading = tagOptions?.Loading ?? (options.EnableLazyLoading ? "lazy" : "eager");
        var decoding = tagOptions?.Decoding ?? "async";
        var cssClass = tagOptions?.CssClass;
        var fetchPriority = tagOptions?.FetchPriority;

        // Generate the default src (largest size)
        var defaultWidth = widths.Max();
        var src = GetOptimizedImageUrl(imageUrl, defaultWidth, tagOptions?.Quality, tagOptions?.Format);

        // Build the img tag
        var attributes = new List<string>
        {
            $"src=\"{src}\"",
            $"srcset=\"{srcset}\"",
            $"sizes=\"{sizes}\"",
            $"alt=\"{System.Web.HttpUtility.HtmlEncode(alt)}\"",
            $"loading=\"{loading}\"",
            $"decoding=\"{decoding}\""
        };

        if (!string.IsNullOrEmpty(cssClass))
        {
            attributes.Add($"class=\"{cssClass}\"");
        }

        if (!string.IsNullOrEmpty(fetchPriority))
        {
            attributes.Add($"fetchpriority=\"{fetchPriority}\"");
        }

        if (!string.IsNullOrEmpty(tagOptions?.AspectRatio))
        {
            attributes.Add($"style=\"aspect-ratio: {tagOptions.AspectRatio}\"");
        }

        // Add custom attributes
        if (tagOptions?.Attributes is not null)
        {
            foreach (var (key, value) in tagOptions.Attributes)
            {
                attributes.Add($"{key}=\"{System.Web.HttpUtility.HtmlEncode(value)}\"");
            }
        }

        return $"<img {string.Join(" ", attributes)} />";
    }

    public string GenerateSrcSet(string imageUrl, IEnumerable<int>? widths = null)
    {
        var effectiveWidths = widths ?? options.DefaultWidths;

        var srcsetEntries = effectiveWidths
            .OrderBy(w => w)
            .Select(width => $"{GetOptimizedImageUrl(imageUrl, width)} {width}w");

        return string.Join(", ", srcsetEntries);
    }

    public string GetOptimizedImageUrl(string imageUrl, int width, int? quality = null, string? format = null)
    {
        var effectiveQuality = quality ?? options.DefaultQuality;
        var effectiveFormat = format ?? options.DefaultFormat;

        // Build XbK image URL with transformations
        // Format: /getmedia/{guid}/{filename}?width={w}&quality={q}&format={f}
        var separator = imageUrl.Contains('?') ? "&" : "?";

        return $"{imageUrl}{separator}width={width}&quality={effectiveQuality}&format={effectiveFormat}";
    }

    /// <summary>
    /// Extracts the image URL from various asset types.
    /// </summary>
    private static string ExtractImageUrl(object asset)
    {
        return asset switch
        {
            ContentItemAsset contentAsset => contentAsset.Url,
            string stringUrl => stringUrl,
            null => string.Empty,
            _ => asset.ToString() ?? string.Empty
        };
    }
}
