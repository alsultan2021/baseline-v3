using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Baseline.Core.Middleware;

/// <summary>
/// Middleware for "Bring Your Own CDN" support.
/// Adds appropriate cache control headers, surrogate keys, and CDN-specific optimizations
/// to enable enterprise customers to use their own CDN with custom edge rules.
/// </summary>
public class CdnMiddleware(
    RequestDelegate next,
    IOptions<BaselineCoreOptions> options,
    IHostEnvironment environment)
{
    private readonly CdnOptions _options = options.Value.Cdn;
    private readonly bool _isDevelopment = environment.IsDevelopment();

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_options.Enabled)
        {
            await next(context);
            return;
        }

        var path = context.Request.Path.Value ?? "";

        // Check if path should bypass CDN
        if (ShouldBypass(path))
        {
            AddBypassHeaders(context);
            await next(context);
            return;
        }

        // Register callback to add CDN headers before response starts
        context.Response.OnStarting(() =>
        {
            AddCdnHeaders(context);
            return Task.CompletedTask;
        });

        await next(context);
    }

    private bool ShouldBypass(string path)
    {
        foreach (var bypassPath in _options.BypassPaths)
        {
            if (path.StartsWith(bypassPath, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private void AddBypassHeaders(HttpContext context)
    {
        var headers = context.Response.Headers;

        // Tell CDN to not cache this response
        headers.TryAdd("Cache-Control", "private, no-store, no-cache, must-revalidate");

        if (_options.EnableSurrogateControl)
        {
            headers.TryAdd("Surrogate-Control", "no-store");
        }

        if (_isDevelopment && _options.EnableDebugHeaders)
        {
            headers.TryAdd(_options.CacheStatusHeader, "BYPASS");
        }
    }

    private void AddCdnHeaders(HttpContext context)
    {
        var headers = context.Response.Headers;
        var path = context.Request.Path.Value ?? "";
        var contentType = context.Response.ContentType ?? "";

        // Don't override if Cache-Control is already set explicitly
        if (headers.ContainsKey("Cache-Control"))
        {
            AddDebugHeaders(context, "PASSTHROUGH");
            return;
        }

        // Determine cache TTL based on content type
        var (ttl, cacheType) = DetermineCacheTtl(path, contentType);

        if (_options.EnableCacheHeaders)
        {
            AddCacheControlHeaders(context, ttl);
        }

        if (_options.EnableSurrogateControl)
        {
            AddSurrogateControlHeaders(context);
        }

        if (_options.EnableCacheKeyHints)
        {
            AddCacheKeyHints(context);
        }

        if (_options.EnableCacheTags)
        {
            AddCacheTags(context);
        }

        AddCustomHeaders(context);
        AddDebugHeaders(context, cacheType);
    }

    private (int ttl, string cacheType) DetermineCacheTtl(string path, string contentType)
    {
        // Static assets (CSS, JS, fonts, images)
        if (IsStaticAsset(path, contentType))
        {
            return (_options.StaticAssetTtlSeconds, "STATIC");
        }

        // Media library files
        if (path.StartsWith("/getmedia", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/getattachment", StringComparison.OrdinalIgnoreCase))
        {
            return (_options.MediaLibraryTtlSeconds, "MEDIA");
        }

        // API endpoints
        if (path.StartsWith("/api", StringComparison.OrdinalIgnoreCase) ||
            contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase))
        {
            return (_options.ApiTtlSeconds, "API");
        }

        // Default page cache
        return (_options.DefaultPageTtlSeconds, "PAGE");
    }

    private static bool IsStaticAsset(string path, string contentType)
    {
        // Check by file extension
        foreach (var ext in StaticExtensions)
        {
            if (path.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        // Check by content type
        foreach (var ct in StaticContentTypes)
        {
            if (contentType.StartsWith(ct, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static readonly string[] StaticExtensions =
    [
        ".css", ".js", ".mjs", ".map",
        ".woff", ".woff2", ".ttf", ".eot", ".otf",
        ".png", ".jpg", ".jpeg", ".gif", ".webp", ".avif", ".svg", ".ico",
        ".mp4", ".webm", ".mp3", ".wav",
        ".pdf", ".zip"
    ];

    private static readonly string[] StaticContentTypes =
    [
        "text/css",
        "text/javascript", "application/javascript",
        "font/", "application/font",
        "image/",
        "audio/", "video/"
    ];

    private void AddCacheControlHeaders(HttpContext context, int ttl)
    {
        var headers = context.Response.Headers;
        var directives = new List<string>
        {
            "public",
            $"max-age={ttl}"
        };

        if (_options.EnableStaleWhileRevalidate && _options.StaleWhileRevalidateSeconds > 0)
        {
            directives.Add($"stale-while-revalidate={_options.StaleWhileRevalidateSeconds}");
        }

        if (_options.EnableStaleIfError && _options.StaleIfErrorSeconds > 0)
        {
            directives.Add($"stale-if-error={_options.StaleIfErrorSeconds}");
        }

        headers.TryAdd("Cache-Control", string.Join(", ", directives));
    }

    private void AddSurrogateControlHeaders(HttpContext context)
    {
        var headers = context.Response.Headers;

        // Surrogate-Control allows CDN edge to cache longer than browser
        headers.TryAdd("Surrogate-Control", $"max-age={_options.SurrogateTtlSeconds}");

        // Indicate request collapsing is supported
        if (_options.EnableRequestCollapsing)
        {
            // Fastly-specific: enable request coalescing
            headers.TryAdd("Fastly-Force-Shield", "1");
        }
    }

    private void AddCacheKeyHints(HttpContext context)
    {
        var headers = context.Response.Headers;

        // Vary header for cache key differentiation
        if (_options.VaryHeaders.Count > 0)
        {
            var existingVary = headers["Vary"].ToString();
            var allVary = string.IsNullOrEmpty(existingVary)
                ? string.Join(", ", _options.VaryHeaders)
                : $"{existingVary}, {string.Join(", ", _options.VaryHeaders)}";

            headers["Vary"] = allVary;
        }

        // Cookie-based vary hints (CDN-specific)
        if (_options.VaryCookies.Count > 0)
        {
            // Fastly uses Vary-Cookie, others may use different mechanisms
            headers.TryAdd("X-Vary-Cookies", string.Join(", ", _options.VaryCookies));
        }

        // Query parameter hints for cache key
        if (_options.IgnoreQueryParams.Count > 0)
        {
            headers.TryAdd("X-Cache-Ignore-Query", string.Join(", ", _options.IgnoreQueryParams));
        }
    }

    private void AddCacheTags(HttpContext context)
    {
        var headers = context.Response.Headers;
        var tags = new List<string>();

        // Add prefix if configured
        var prefix = _options.CacheTagPrefix ?? "";

        // Add path-based tag
        var path = context.Request.Path.Value ?? "/";
        var pathTag = path.Replace("/", "_").Trim('_');
        if (!string.IsNullOrEmpty(pathTag))
        {
            tags.Add($"{prefix}path_{pathTag}");
        }

        // Add content type tag from response
        var contentType = context.Response.ContentType?.Split(';')[0].Trim() ?? "unknown";
        var contentTypeTag = contentType.Replace("/", "_");
        tags.Add($"{prefix}type_{contentTypeTag}");

        // Check for custom cache tags added by handlers
        if (context.Items.TryGetValue("CdnCacheTags", out var customTags) && customTags is List<string> customTagList)
        {
            foreach (var tag in customTagList)
            {
                tags.Add($"{prefix}{tag}");
            }
        }

        // Limit tags to max allowed
        var finalTags = tags.Take(_options.MaxCacheTags);

        if (finalTags.Any())
        {
            headers.TryAdd(_options.CacheTagHeader, string.Join(" ", finalTags));
        }
    }

    private void AddCustomHeaders(HttpContext context)
    {
        var headers = context.Response.Headers;

        foreach (var (key, value) in _options.CustomHeaders)
        {
            headers.TryAdd(key, value);
        }

        // Add provider hint
        if (!string.IsNullOrEmpty(_options.Provider))
        {
            headers.TryAdd("X-CDN-Provider", _options.Provider);
        }

        // Add origin hint for debugging
        if (!string.IsNullOrEmpty(_options.OriginBaseUrl))
        {
            headers.TryAdd("X-Origin", _options.OriginBaseUrl);
        }
    }

    private void AddDebugHeaders(HttpContext context, string cacheType)
    {
        if (!_isDevelopment || !_options.EnableDebugHeaders)
        {
            return;
        }

        var headers = context.Response.Headers;
        headers.TryAdd(_options.CacheStatusHeader, $"ORIGIN-{cacheType}");
        headers.TryAdd("X-CDN-Enabled", "true");
        headers.TryAdd("X-CDN-Config", $"provider={_options.Provider ?? "custom"};swr={_options.EnableStaleWhileRevalidate}");
    }
}

/// <summary>
/// Extension methods for adding CDN middleware.
/// </summary>
public static class CdnMiddlewareExtensions
{
    /// <summary>
    /// Adds CDN middleware to the pipeline.
    /// Should be added early in the pipeline, after routing but before other middleware.
    /// </summary>
    public static IApplicationBuilder UseCdnMiddleware(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CdnMiddleware>();
    }
}

/// <summary>
/// Extension methods for adding cache tags to HttpContext.
/// </summary>
public static class CdnCacheTagExtensions
{
    /// <summary>
    /// Adds a cache tag for CDN surrogate key purging.
    /// Tags are used by CDNs like Fastly and Cloudflare for targeted cache invalidation.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="tag">The cache tag to add (e.g., "article_123", "category_news").</param>
    public static void AddCdnCacheTag(this HttpContext context, string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return;
        }

        if (!context.Items.TryGetValue("CdnCacheTags", out var existing))
        {
            context.Items["CdnCacheTags"] = new List<string> { tag };
        }
        else if (existing is List<string> tags)
        {
            tags.Add(tag);
        }
    }

    /// <summary>
    /// Adds multiple cache tags for CDN surrogate key purging.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="tags">The cache tags to add.</param>
    public static void AddCdnCacheTags(this HttpContext context, IEnumerable<string> tags)
    {
        foreach (var tag in tags)
        {
            context.AddCdnCacheTag(tag);
        }
    }

    /// <summary>
    /// Adds cache tags for a content item based on its type and ID.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="contentTypeName">The content type name.</param>
    /// <param name="contentItemId">The content item ID.</param>
    public static void AddCdnCacheTagForContent(this HttpContext context, string contentTypeName, int contentItemId)
    {
        context.AddCdnCacheTag($"ct_{contentTypeName}");
        context.AddCdnCacheTag($"ci_{contentItemId}");
    }
}
