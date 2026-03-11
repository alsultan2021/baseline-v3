using Baseline.Core.Models;
using CMS.Websites.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using XperienceCommunity.ChannelSettings.Repositories;

namespace Baseline.Core.Services;

/// <summary>
/// Service interface for retrieving Baseline Core options.
/// Merges UI-configured channel settings with code-based options.
/// </summary>
public interface ICoreOptionsProvider
{
    /// <summary>
    /// Gets the effective BaselineCoreOptions for the current channel.
    /// UI settings override code-based defaults where configured.
    /// </summary>
    Task<BaselineCoreOptions> GetOptionsAsync();

    /// <summary>
    /// Gets the effective CdnOptions for the current channel.
    /// </summary>
    Task<CdnOptions> GetCdnOptionsAsync();

    /// <summary>
    /// Gets the effective SecurityHeadersOptions for the current channel.
    /// </summary>
    Task<SecurityHeadersOptions> GetSecurityHeadersOptionsAsync();

    /// <summary>
    /// Gets the core channel settings from the database.
    /// Returns null if no settings are configured for the current channel.
    /// </summary>
    Task<CoreChannelSettings?> GetChannelSettingsAsync();
}

/// <summary>
/// Provides Baseline Core options by merging UI channel settings with code-based configuration.
/// Channel settings (from admin UI) take precedence over code-based settings in Program.cs.
/// </summary>
public class CoreOptionsProvider(
    IChannelCustomSettingsRepository channelSettingsRepository,
    IWebsiteChannelContext websiteChannelContext,
    IOptions<BaselineCoreOptions> codeOptions,
    ILogger<CoreOptionsProvider> logger) : ICoreOptionsProvider
{
    private readonly BaselineCoreOptions _codeOptions = codeOptions.Value;
    private CoreChannelSettings? _cachedChannelSettings;
    private bool _channelSettingsLoaded;

    /// <inheritdoc />
    public async Task<BaselineCoreOptions> GetOptionsAsync()
    {
        var channelSettings = await GetChannelSettingsAsync();
        if (channelSettings == null)
        {
            return _codeOptions;
        }

        // Create a merged options object
        var merged = new BaselineCoreOptions
        {
            // SEO Features
            EnableStructuredData = channelSettings.SeoEnableStructuredData,
            EnableRobotsTxt = channelSettings.SeoEnableRobotsTxt,
            EnableLlmsTxt = channelSettings.SeoEnableLlmsTxt,

            // Responsive Images
            EnableResponsiveImages = channelSettings.ResponsiveImagesEnabled,

            // Security Headers
            EnableSecurityHeaders = channelSettings.SecurityHeadersEnabled,

            // Output Cache
            EnableOutputCache = channelSettings.OutputCacheEnabled,

            // Compression
            EnableCompression = channelSettings.CompressionEnabled,

            // Copy non-UI configurable options from code
            EnableSecurityTxt = _codeOptions.EnableSecurityTxt,
            EnableHtmlMinification = _codeOptions.EnableHtmlMinification,
            EnableAdsTxt = _codeOptions.EnableAdsTxt,
            EnableFeatureFolderViewEngine = _codeOptions.EnableFeatureFolderViewEngine,
            EnableUrlHelper = _codeOptions.EnableUrlHelper,
            EnableHtmlEncoder = _codeOptions.EnableHtmlEncoder,
            EnableHealthChecks = _codeOptions.EnableHealthChecks,
            EnableMiniProfiler = _codeOptions.EnableMiniProfiler,

            // Complex options - merge where UI provides values
            RobotsTxt = _codeOptions.RobotsTxt,
            LlmsTxt = _codeOptions.LlmsTxt,
            SecurityTxt = _codeOptions.SecurityTxt,
            AdsTxt = _codeOptions.AdsTxt,
            Geo = _codeOptions.Geo,
            SeoAudit = _codeOptions.SeoAudit,

            // Merge CDN options
            Cdn = await GetCdnOptionsAsync(),

            // Merge Security Headers
            SecurityHeaders = await GetSecurityHeadersOptionsAsync(),

            // Merge Responsive Images
            ResponsiveImages = new ResponsiveImageOptions
            {
                DefaultQuality = channelSettings.ResponsiveImagesDefaultQuality,
                DefaultFormat = channelSettings.ResponsiveImagesDefaultFormat,
                DefaultWidths = _codeOptions.ResponsiveImages.DefaultWidths,
                EnableLazyLoading = _codeOptions.ResponsiveImages.EnableLazyLoading,
                DefaultSizes = _codeOptions.ResponsiveImages.DefaultSizes
            },

            // Merge Output Cache
            OutputCache = new OutputCacheOptions
            {
                DefaultExpirationMinutes = channelSettings.OutputCacheDefaultExpirationMinutes,
                StaticAssetExpirationDays = _codeOptions.OutputCache.StaticAssetExpirationDays
            },

            // Copy compression options
            Compression = _codeOptions.Compression
        };

        return merged;
    }

    /// <inheritdoc />
    public async Task<CdnOptions> GetCdnOptionsAsync()
    {
        var channelSettings = await GetChannelSettingsAsync();
        if (channelSettings == null)
        {
            return _codeOptions.Cdn;
        }

        return new CdnOptions
        {
            Enabled = channelSettings.CdnEnabled,
            Provider = string.IsNullOrEmpty(channelSettings.CdnProvider) ? _codeOptions.Cdn.Provider : channelSettings.CdnProvider,
            OriginBaseUrl = string.IsNullOrEmpty(channelSettings.CdnOriginBaseUrl) ? _codeOptions.Cdn.OriginBaseUrl : channelSettings.CdnOriginBaseUrl,
            EdgeBaseUrl = string.IsNullOrEmpty(channelSettings.CdnEdgeBaseUrl) ? _codeOptions.Cdn.EdgeBaseUrl : channelSettings.CdnEdgeBaseUrl,
            EnableCacheHeaders = channelSettings.CdnEnableCacheHeaders,
            DefaultPageTtlSeconds = channelSettings.CdnDefaultPageTtlSeconds,
            StaticAssetTtlSeconds = channelSettings.CdnStaticAssetTtlSeconds,
            MediaLibraryTtlSeconds = channelSettings.CdnMediaLibraryTtlSeconds,
            EnableStaleWhileRevalidate = channelSettings.CdnEnableStaleWhileRevalidate,
            EnableCacheTags = channelSettings.CdnEnableCacheTags,
            CacheTagHeader = string.IsNullOrEmpty(channelSettings.CdnCacheTagHeader) ? _codeOptions.Cdn.CacheTagHeader : channelSettings.CdnCacheTagHeader,
            CacheTagPrefix = string.IsNullOrEmpty(channelSettings.CdnCacheTagPrefix) ? _codeOptions.Cdn.CacheTagPrefix : channelSettings.CdnCacheTagPrefix,

            // Copy non-UI options from code
            StaleWhileRevalidateSeconds = _codeOptions.Cdn.StaleWhileRevalidateSeconds,
            EnableStaleIfError = _codeOptions.Cdn.EnableStaleIfError,
            StaleIfErrorSeconds = _codeOptions.Cdn.StaleIfErrorSeconds,
            ApiTtlSeconds = _codeOptions.Cdn.ApiTtlSeconds,
            EnableSurrogateControl = _codeOptions.Cdn.EnableSurrogateControl,
            SurrogateTtlSeconds = _codeOptions.Cdn.SurrogateTtlSeconds,
            EnableCacheKeyHints = _codeOptions.Cdn.EnableCacheKeyHints,
            VaryHeaders = _codeOptions.Cdn.VaryHeaders,
            VaryCookies = _codeOptions.Cdn.VaryCookies,
            VaryQueryParams = _codeOptions.Cdn.VaryQueryParams,
            IgnoreQueryParams = _codeOptions.Cdn.IgnoreQueryParams,
            EnablePurgeHints = _codeOptions.Cdn.EnablePurgeHints,
            MaxCacheTags = _codeOptions.Cdn.MaxCacheTags,
            EnableGeoHeaders = _codeOptions.Cdn.EnableGeoHeaders,
            GeoHeaderMappings = _codeOptions.Cdn.GeoHeaderMappings,
            EnableDeviceHeaders = _codeOptions.Cdn.EnableDeviceHeaders,
            BypassPaths = _codeOptions.Cdn.BypassPaths,
            EnableEsiHints = _codeOptions.Cdn.EnableEsiHints,
            EsiTagFormat = _codeOptions.Cdn.EsiTagFormat,
            EnableRequestCollapsing = _codeOptions.Cdn.EnableRequestCollapsing,
            EnablePrefetchHints = _codeOptions.Cdn.EnablePrefetchHints,
            CustomHeaders = _codeOptions.Cdn.CustomHeaders,
            EnableDebugHeaders = _codeOptions.Cdn.EnableDebugHeaders,
            CacheStatusHeader = _codeOptions.Cdn.CacheStatusHeader
        };
    }

    /// <inheritdoc />
    public async Task<SecurityHeadersOptions> GetSecurityHeadersOptionsAsync()
    {
        var channelSettings = await GetChannelSettingsAsync();
        if (channelSettings == null)
        {
            return _codeOptions.SecurityHeaders;
        }

        return new SecurityHeadersOptions
        {
            Enabled = channelSettings.SecurityHeadersEnabled,
            XFrameOptions = string.IsNullOrEmpty(channelSettings.SecurityHeadersXFrameOptions) ? _codeOptions.SecurityHeaders.XFrameOptions : channelSettings.SecurityHeadersXFrameOptions,
            ReferrerPolicy = string.IsNullOrEmpty(channelSettings.SecurityHeadersReferrerPolicy) ? _codeOptions.SecurityHeaders.ReferrerPolicy : channelSettings.SecurityHeadersReferrerPolicy,
            StrictTransportSecurity = channelSettings.SecurityHeadersStrictTransportSecurity,
            // Fall back to code-configured max-age when channel settings value is 0 (effectively disables HSTS)
            HstsMaxAgeSeconds = channelSettings.SecurityHeadersHstsMaxAgeSeconds > 0
                ? channelSettings.SecurityHeadersHstsMaxAgeSeconds
                : _codeOptions.SecurityHeaders.HstsMaxAgeSeconds,
            ContentSecurityPolicy = string.IsNullOrEmpty(channelSettings.SecurityHeadersContentSecurityPolicy) ? _codeOptions.SecurityHeaders.ContentSecurityPolicy : channelSettings.SecurityHeadersContentSecurityPolicy,

            // Copy non-UI options from code
            XContentTypeOptions = _codeOptions.SecurityHeaders.XContentTypeOptions,
            XXssProtection = _codeOptions.SecurityHeaders.XXssProtection,
            PermissionsPolicy = _codeOptions.SecurityHeaders.PermissionsPolicy,
            HstsIncludeSubdomains = _codeOptions.SecurityHeaders.HstsIncludeSubdomains,
            HstsPreload = _codeOptions.SecurityHeaders.HstsPreload,
            CrossOriginOpenerPolicy = _codeOptions.SecurityHeaders.CrossOriginOpenerPolicy,
            CrossOriginResourcePolicy = _codeOptions.SecurityHeaders.CrossOriginResourcePolicy,
            CrossOriginEmbedderPolicy = _codeOptions.SecurityHeaders.CrossOriginEmbedderPolicy
        };
    }

    /// <inheritdoc />
    public async Task<CoreChannelSettings?> GetChannelSettingsAsync()
    {
        // Return cached value if already loaded
        if (_channelSettingsLoaded)
        {
            return _cachedChannelSettings;
        }

        try
        {
            var channelName = websiteChannelContext.WebsiteChannelName;
            if (string.IsNullOrEmpty(channelName))
            {
                _channelSettingsLoaded = true;
                return null;
            }

            _cachedChannelSettings = await channelSettingsRepository.GetSettingsModel<CoreChannelSettings>();
            _channelSettingsLoaded = true;
            return _cachedChannelSettings;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "CoreOptionsProvider: failed to retrieve channel settings for '{Channel}', falling back to code options", websiteChannelContext.WebsiteChannelName);
            _channelSettingsLoaded = true;
            return null;
        }
    }
}
