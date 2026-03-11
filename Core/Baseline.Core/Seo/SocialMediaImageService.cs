using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Baseline.Core.Seo;

/// <summary>
/// Service for generating optimized social media images for Open Graph and Twitter Cards.
/// Supports platform-specific image dimensions and optimization.
/// </summary>
public interface ISocialMediaImageService
{
    /// <summary>
    /// Generates optimized image URLs for different social media platforms.
    /// </summary>
    /// <param name="originalImageUrl">The original image URL.</param>
    /// <returns>Optimized image URLs for different platforms.</returns>
    Task<SocialMediaImageUrls> GenerateOptimizedImagesAsync(string originalImageUrl);

    /// <summary>
    /// Converts a relative URL to an absolute URL.
    /// </summary>
    /// <param name="relativeUrl">The relative URL.</param>
    /// <returns>The absolute URL.</returns>
    string GetAbsoluteUrl(string? relativeUrl);

    /// <summary>
    /// Generates an optimized image URL for a specific platform.
    /// </summary>
    /// <param name="originalImageUrl">The original image URL.</param>
    /// <param name="platform">The social media platform.</param>
    /// <returns>The optimized image URL.</returns>
    string GenerateOptimizedImageUrl(string originalImageUrl, SocialMediaPlatform platform);
}

/// <summary>
/// Supported social media platforms for image optimization.
/// </summary>
public enum SocialMediaPlatform
{
    /// <summary>Facebook/Meta (1200x630).</summary>
    Facebook,
    /// <summary>Twitter/X (1024x512 or 1200x675 for summary_large_image).</summary>
    Twitter,
    /// <summary>LinkedIn (1200x627).</summary>
    LinkedIn,
    /// <summary>Pinterest (1000x1500 for ideal vertical).</summary>
    Pinterest,
    /// <summary>Instagram (1080x1080 square).</summary>
    Instagram
}

/// <summary>
/// Container for optimized social media image URLs.
/// </summary>
public sealed record SocialMediaImageUrls
{
    /// <summary>Facebook-optimized image URL (1200x630).</summary>
    public string FacebookUrl { get; init; } = string.Empty;

    /// <summary>Twitter-optimized image URL (1024x512).</summary>
    public string TwitterUrl { get; init; } = string.Empty;

    /// <summary>LinkedIn-optimized image URL (1200x627).</summary>
    public string LinkedInUrl { get; init; } = string.Empty;

    /// <summary>Original image URL as absolute URL.</summary>
    public string OriginalUrl { get; init; } = string.Empty;
}

/// <summary>
/// Default implementation of social media image optimization service.
/// Uses query parameter approach compatible with XperienceCommunity.ImageProcessing.
/// </summary>
internal sealed class SocialMediaImageService(
    IHttpContextAccessor httpContextAccessor,
    IConfiguration configuration) : ISocialMediaImageService
{
    /// <summary>
    /// Platform-specific image dimensions (platform, width, height).
    /// </summary>
    private static readonly Dictionary<SocialMediaPlatform, (int Width, int Height)> PlatformDimensions = new()
    {
        [SocialMediaPlatform.Facebook] = (1200, 630),
        [SocialMediaPlatform.Twitter] = (1024, 512),
        [SocialMediaPlatform.LinkedIn] = (1200, 627),
        [SocialMediaPlatform.Pinterest] = (1000, 1500),
        [SocialMediaPlatform.Instagram] = (1080, 1080)
    };

    public Task<SocialMediaImageUrls> GenerateOptimizedImagesAsync(string originalImageUrl)
    {
        bool enableOptimization = configuration.GetValue("Baseline:SocialMedia:EnableImageOptimization", true);

        if (!enableOptimization || string.IsNullOrEmpty(originalImageUrl))
        {
            var absoluteUrl = GetAbsoluteUrl(originalImageUrl);
            return Task.FromResult(new SocialMediaImageUrls
            {
                OriginalUrl = absoluteUrl,
                FacebookUrl = absoluteUrl,
                TwitterUrl = absoluteUrl,
                LinkedInUrl = absoluteUrl
            });
        }

        return Task.FromResult(new SocialMediaImageUrls
        {
            OriginalUrl = GetAbsoluteUrl(originalImageUrl),
            FacebookUrl = GenerateOptimizedImageUrl(originalImageUrl, SocialMediaPlatform.Facebook),
            TwitterUrl = GenerateOptimizedImageUrl(originalImageUrl, SocialMediaPlatform.Twitter),
            LinkedInUrl = GenerateOptimizedImageUrl(originalImageUrl, SocialMediaPlatform.LinkedIn)
        });
    }

    public string GetAbsoluteUrl(string? relativeUrl)
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

    public string GenerateOptimizedImageUrl(string originalImageUrl, SocialMediaPlatform platform)
    {
        if (string.IsNullOrEmpty(originalImageUrl))
        {
            return string.Empty;
        }

        if (!PlatformDimensions.TryGetValue(platform, out var dimensions))
        {
            return GetAbsoluteUrl(originalImageUrl);
        }

        var quality = configuration.GetValue("Baseline:SocialMedia:ImageQuality", 85);
        var format = configuration.GetValue("Baseline:SocialMedia:ImageFormat", "jpg");

        var queryParams = HttpUtility.ParseQueryString(string.Empty);
        queryParams["width"] = dimensions.Width.ToString();
        queryParams["height"] = dimensions.Height.ToString();
        queryParams["mode"] = "crop";
        queryParams["format"] = format;
        queryParams["quality"] = quality.ToString();

        var separator = originalImageUrl.Contains('?') ? "&" : "?";
        return GetAbsoluteUrl($"{originalImageUrl}{separator}{queryParams}");
    }
}
