using System.Collections.Concurrent;
using System.Reflection;

namespace Baseline.Core;

/// <summary>
/// Shared helper for extracting OG image URLs from <see cref="IGenericHasImage"/> instances.
/// Consolidates fallback logic used by both MetaDataService and SeoMetadataService.
/// </summary>
internal static class OgImageHelper
{
    private static readonly ConcurrentDictionary<(Type, string), PropertyInfo?> _propCache = new();

    /// <summary>
    /// Extracts image URL from an <see cref="IGenericHasImage"/> with full fallback chain:
    /// 1. <see cref="IGenericHasImage.HasImageImage"/> Url
    /// 2. ImageAsset / Asset / LogoImageAsset → Url
    /// 3. Direct Url property
    /// </summary>
    internal static string? ExtractImageUrl(IGenericHasImage? ogImage)
    {
        if (ogImage is null)
            return null;

        // Try the interface directly
        if (!string.IsNullOrEmpty(ogImage.HasImageImage?.Url))
            return ogImage.HasImageImage.Url;

        // Fallback: Try common image asset property patterns
        var imageType = ogImage.GetType();

        var assetProperty = _propCache.GetOrAdd((imageType, "ImageAsset"), static k => k.Item1.GetProperty(k.Item2))
            ?? _propCache.GetOrAdd((imageType, "Asset"), static k => k.Item1.GetProperty(k.Item2))
            ?? _propCache.GetOrAdd((imageType, "LogoImageAsset"), static k => k.Item1.GetProperty(k.Item2));

        if (assetProperty?.GetValue(ogImage) is { } asset)
        {
            var urlProperty = _propCache.GetOrAdd((asset.GetType(), "Url"), static k => k.Item1.GetProperty(k.Item2));
            if (urlProperty?.GetValue(asset) is string url && !string.IsNullOrEmpty(url))
                return url;
        }

        // Try direct Url property
        var directUrlProperty = _propCache.GetOrAdd((imageType, "Url"), static k => k.Item1.GetProperty(k.Item2));
        if (directUrlProperty?.GetValue(ogImage) is string directUrl && !string.IsNullOrEmpty(directUrl))
            return directUrl;

        return null;
    }
}
