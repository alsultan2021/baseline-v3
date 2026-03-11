using CMS.Websites.Routing;

namespace Baseline.Core;

/// <summary>
/// Service for handling preview mode utilities and cache-safe operations.
/// </summary>
public class PreviewService(IWebsiteChannelContext websiteChannelContext) : IPreviewService
{
    private readonly IWebsiteChannelContext _websiteChannelContext = websiteChannelContext;

    /// <inheritdoc />
    public bool IsPreviewMode => _websiteChannelContext.IsPreview;

    /// <inheritdoc />
    public bool ShouldDisableCache => _websiteChannelContext.IsPreview;

    /// <inheritdoc />
    public PreviewSafeCacheSettings GetCacheSettings(int defaultCacheMinutes = 10)
    {
        return new PreviewSafeCacheSettings
        {
            CacheEnabled = !IsPreviewMode,
            CacheMinutes = IsPreviewMode ? 0 : defaultCacheMinutes,
            IsPreview = IsPreviewMode
        };
    }

    /// <inheritdoc />
    public bool ApplyPreviewContext(bool? forPreview = null)
    {
        return forPreview ?? IsPreviewMode;
    }
}
