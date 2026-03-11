namespace Baseline.Core;

/// <summary>
/// Service for handling preview mode utilities and cache-safe operations.
/// </summary>
public interface IPreviewService
{
    /// <summary>
    /// Gets whether the current request is in preview mode.
    /// </summary>
    bool IsPreviewMode { get; }

    /// <summary>
    /// Gets whether caching should be disabled for the current request.
    /// </summary>
    bool ShouldDisableCache { get; }

    /// <summary>
    /// Gets preview-safe cache settings based on current mode.
    /// </summary>
    /// <param name="defaultCacheMinutes">Default cache duration when not in preview.</param>
    /// <returns>Cache settings adjusted for preview mode.</returns>
    PreviewSafeCacheSettings GetCacheSettings(int defaultCacheMinutes = 10);

    /// <summary>
    /// Applies preview context to content query options.
    /// </summary>
    /// <param name="forPreview">Whether to force preview mode (optional override).</param>
    /// <returns>True if preview mode should be applied.</returns>
    bool ApplyPreviewContext(bool? forPreview = null);
}

/// <summary>
/// Cache settings that are safe for preview mode.
/// </summary>
public sealed record PreviewSafeCacheSettings
{
    /// <summary>
    /// Whether caching is enabled.
    /// </summary>
    public bool CacheEnabled { get; init; }

    /// <summary>
    /// Cache duration in minutes (0 if caching disabled).
    /// </summary>
    public int CacheMinutes { get; init; }

    /// <summary>
    /// Whether in preview mode.
    /// </summary>
    public bool IsPreview { get; init; }
}
