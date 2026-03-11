namespace Baseline.Core;

/// <summary>
/// Configuration options for Baseline v3 Core module.
/// Use with <see cref="BaselineServiceCollectionExtensions.AddBaselineCore"/>.
/// </summary>
public class BaselineCoreOptions
{
    /// <summary>
    /// Enable structured data (JSON-LD) generation for SEO.
    /// Default: true
    /// </summary>
    public bool EnableStructuredData { get; set; } = true;

    /// <summary>
    /// Enable responsive image tag helper with srcset generation.
    /// Default: true
    /// </summary>
    public bool EnableResponsiveImages { get; set; } = true;

    /// <summary>
    /// Enable security headers middleware (X-Frame-Options, CSP, etc.).
    /// Default: true
    /// </summary>
    public bool EnableSecurityHeaders { get; set; } = true;

    /// <summary>
    /// Enable robots.txt endpoint generation.
    /// Default: true
    /// </summary>
    public bool EnableRobotsTxt { get; set; } = true;

    /// <summary>
    /// Enable llms.txt endpoint for AI crawlers.
    /// Default: true
    /// </summary>
    public bool EnableLlmsTxt { get; set; } = true;

    /// <summary>
    /// Enable security.txt endpoint.
    /// Default: false (requires configuration)
    /// </summary>
    public bool EnableSecurityTxt { get; set; } = false;

    /// <summary>
    /// Enable HTML minification in production.
    /// Default: true
    /// </summary>
    public bool EnableHtmlMinification { get; set; } = true;

    /// <summary>
    /// Configuration for robots.txt generation.
    /// </summary>
    public RobotsTxtOptions RobotsTxt { get; set; } = new();

    /// <summary>
    /// Configuration for llms.txt generation.
    /// </summary>
    public LlmsTxtOptions LlmsTxt { get; set; } = new();

    /// <summary>
    /// Configuration for security.txt generation.
    /// </summary>
    public SecurityTxtOptions SecurityTxt { get; set; } = new();

    /// <summary>
    /// Configuration for security headers.
    /// </summary>
    public SecurityHeadersOptions SecurityHeaders { get; set; } = new();

    /// <summary>
    /// Configuration for responsive images.
    /// </summary>
    public ResponsiveImageOptions ResponsiveImages { get; set; } = new();

    /// <summary>
    /// Configuration for ads.txt generation.
    /// </summary>
    public AdsTxtOptions AdsTxt { get; set; } = new();

    /// <summary>
    /// Configuration for GEO (Generative Engine Optimization).
    /// </summary>
    public GeoOptimizationOptions Geo { get; set; } = new();

    /// <summary>
    /// Configuration for SEO auditing.
    /// </summary>
    public SeoAuditingOptions SeoAudit { get; set; } = new();

    /// <summary>
    /// Enable ads.txt endpoint.
    /// Default: false
    /// </summary>
    public bool EnableAdsTxt { get; set; } = false;

    /// <summary>
    /// Enable feature folder view engine for organizing views by feature.
    /// Views are resolved from /Features/{Feature}/{View}.cshtml
    /// Default: false (opt-in)
    /// </summary>
    public bool EnableFeatureFolderViewEngine { get; set; } = false;

    /// <summary>
    /// Enable IUrlHelper scoped registration.
    /// Fixes the common ASP.NET Core issue where IUrlHelper is not directly injectable.
    /// Default: true
    /// </summary>
    public bool EnableUrlHelper { get; set; } = true;

    /// <summary>
    /// Enable Baseline HTML encoder configuration.
    /// Prevents encoding of common URL characters (+, /, =, &amp;, ?).
    /// Default: true
    /// </summary>
    public bool EnableHtmlEncoder { get; set; } = true;

    /// <summary>
    /// Enable Baseline output cache policies.
    /// Adds "default", "static-assets", "no-cache", and "api" policies.
    /// Default: true
    /// </summary>
    public bool EnableOutputCache { get; set; } = true;

    /// <summary>
    /// Enable Baseline compression (Brotli + Gzip) services.
    /// Default: true
    /// </summary>
    public bool EnableCompression { get; set; } = true;

    /// <summary>
    /// Enable health check endpoint at /status.
    /// Default: true
    /// </summary>
    public bool EnableHealthChecks { get; set; } = true;

    /// <summary>
    /// Configuration for output cache.
    /// </summary>
    public OutputCacheOptions OutputCache { get; set; } = new();

    /// <summary>
    /// Configuration for compression.
    /// </summary>
    public BaselineCompressionOptions Compression { get; set; } = new();

    /// <summary>
    /// Configuration for "Bring Your Own CDN" support.
    /// Enables customers to use their own CDN with custom edge rules and configurations.
    /// </summary>
    public CdnOptions Cdn { get; set; } = new();

    /// <summary>
    /// Enable MiniProfiler for development debugging.
    /// Only active in Development environment.
    /// Default: false
    /// </summary>
    public bool EnableMiniProfiler { get; set; } = false;
}

/// <summary>
/// Configuration for robots.txt generation.
/// </summary>
public class RobotsTxtOptions
{
    /// <summary>
    /// Custom rules to add to robots.txt.
    /// </summary>
    public List<RobotsTxtRule> Rules { get; set; } = [];

    /// <summary>
    /// Whether to automatically include sitemap URL.
    /// Default: true
    /// </summary>
    public bool IncludeSitemap { get; set; } = true;

    /// <summary>
    /// Additional sitemap URLs to include.
    /// </summary>
    public List<string> AdditionalSitemaps { get; set; } = [];

    /// <summary>
    /// Whether to generate language-specific sitemaps.
    /// When true, generates sitemap URLs like /sitemap-en.xml, /sitemap-fr.xml
    /// Default: false (generates single /sitemap.xml)
    /// </summary>
    public bool EnableLanguageSpecificSitemaps { get; set; } = false;

    /// <summary>
    /// List of language codes to include in language-specific sitemaps.
    /// Only used when EnableLanguageSpecificSitemaps is true.
    /// If empty, all available languages from Kentico will be used.
    /// Example: ["en", "fr", "es"]
    /// </summary>
    public List<string> SitemapLanguages { get; set; } = [];

    /// <summary>
    /// Base URL for sitemaps. If not set, will be auto-detected from request context.
    /// Example: "https://example.com"
    /// </summary>
    public string? SitemapBaseUrl { get; set; }
}

/// <summary>
/// A single rule for robots.txt.
/// </summary>
public class RobotsTxtRule
{
    public string UserAgent { get; set; } = "*";
    public List<string> Allow { get; set; } = [];
    public List<string> Disallow { get; set; } = [];
    public int? CrawlDelay { get; set; }
}

/// <summary>
/// Configuration for llms.txt generation with GEO (Generative Engine Optimization) support.
/// </summary>
public class LlmsTxtOptions
{
    /// <summary>
    /// Site name displayed in llms.txt.
    /// </summary>
    public string SiteName { get; set; } = string.Empty;

    /// <summary>
    /// Site description for AI crawlers.
    /// </summary>
    public string SiteDescription { get; set; } = string.Empty;

    /// <summary>
    /// Contact email for AI-related inquiries.
    /// </summary>
    public string? ContactEmail { get; set; }

    /// <summary>
    /// Contact URL for AI-related inquiries.
    /// </summary>
    public string? ContactUrl { get; set; }

    /// <summary>
    /// Support URL for users.
    /// </summary>
    public string? SupportUrl { get; set; }

    /// <summary>
    /// Custom sections to include in llms.txt.
    /// </summary>
    public List<LlmsTxtSection> Sections { get; set; } = [];

    /// <summary>
    /// Whether to auto-discover pages for llms.txt.
    /// Default: true
    /// </summary>
    public bool AutoDiscoverPages { get; set; } = true;

    /// <summary>
    /// Content types to include when auto-discovering.
    /// </summary>
    public List<string> IncludedContentTypes { get; set; } = [];

    /// <summary>
    /// Maximum pages to include in auto-discovery.
    /// Default: 100
    /// </summary>
    public int MaxPages { get; set; } = 100;

    // ===== GEO Enhancement Properties =====

    /// <summary>
    /// Version of the llms.txt content for AI model tracking.
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// Last updated date string.
    /// </summary>
    public string? LastUpdated { get; set; }

    /// <summary>
    /// Primary topics/categories the site covers.
    /// </summary>
    public List<string> PrimaryTopics { get; set; } = [];

    /// <summary>
    /// Target audience description.
    /// </summary>
    public string? TargetAudience { get; set; }

    /// <summary>
    /// Whether to include a capabilities section.
    /// </summary>
    public bool EnableCapabilitiesSection { get; set; } = true;

    /// <summary>
    /// Supported languages on the site.
    /// </summary>
    public List<string> SupportedLanguages { get; set; } = [];

    /// <summary>
    /// Content formats available (articles, videos, podcasts, etc.).
    /// </summary>
    public List<string> ContentFormats { get; set; } = [];

    /// <summary>
    /// Site features to highlight.
    /// </summary>
    public List<string> Features { get; set; } = [];

    /// <summary>
    /// API endpoints to document for AI integration.
    /// </summary>
    public List<LlmsTxtApiEndpointOption> ApiEndpoints { get; set; } = [];

    // ===== Vector Index / RAG Support =====

    /// <summary>
    /// Enable vector index section for RAG-capable AI assistants.
    /// </summary>
    public bool EnableVectorIndex { get; set; }

    /// <summary>
    /// URL to the vector index or embedding service.
    /// </summary>
    public string? VectorIndexUrl { get; set; }

    /// <summary>
    /// Format of the vector index (e.g., "FAISS", "Pinecone", "Chroma").
    /// </summary>
    public string? VectorIndexFormat { get; set; }

    /// <summary>
    /// Vector dimensions used.
    /// </summary>
    public int VectorDimensions { get; set; }

    /// <summary>
    /// Embedding model used (e.g., "text-embedding-ada-002").
    /// </summary>
    public string? EmbeddingModel { get; set; }

    // ===== Licensing and Terms =====

    /// <summary>
    /// License information for content usage by AI.
    /// </summary>
    public string? LicenseInfo { get; set; }

    /// <summary>
    /// Allowed use cases for AI consumption of content.
    /// </summary>
    public List<string> AllowedUseCases { get; set; } = [];

    /// <summary>
    /// Restricted use cases for AI consumption.
    /// </summary>
    public List<string> RestrictedUseCases { get; set; } = [];
}

/// <summary>
/// API endpoint option for llms.txt configuration.
/// </summary>
public class LlmsTxtApiEndpointOption
{
    /// <summary>Endpoint name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Endpoint URL.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>HTTP method.</summary>
    public string Method { get; set; } = "GET";

    /// <summary>Description.</summary>
    public string? Description { get; set; }

    /// <summary>Authentication type.</summary>
    public string? Authentication { get; set; }
}

/// <summary>
/// A section in llms.txt.
/// </summary>
public class LlmsTxtSection
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<string> Links { get; set; } = [];
}

/// <summary>
/// Configuration for security.txt generation.
/// </summary>
public class SecurityTxtOptions
{
    /// <summary>
    /// Contact email or URL for security issues.
    /// Required for security.txt to be generated.
    /// </summary>
    public string? Contact { get; set; }

    /// <summary>
    /// Expiration date for the security.txt file.
    /// </summary>
    public DateTimeOffset? Expires { get; set; }

    /// <summary>
    /// URL to the security policy.
    /// </summary>
    public string? Policy { get; set; }

    /// <summary>
    /// PGP key URL or fingerprint.
    /// </summary>
    public string? Encryption { get; set; }

    /// <summary>
    /// Acknowledgments page URL.
    /// </summary>
    public string? Acknowledgments { get; set; }

    /// <summary>
    /// Preferred languages for security reports.
    /// </summary>
    public List<string> PreferredLanguages { get; set; } = ["en"];

    /// <summary>
    /// Canonical URL for the security.txt file.
    /// </summary>
    public string? Canonical { get; set; }

    /// <summary>
    /// Hiring page URL for security positions.
    /// </summary>
    public string? Hiring { get; set; }
}

/// <summary>
/// Configuration for security headers middleware.
/// </summary>
public class SecurityHeadersOptions
{
    /// <summary>
    /// Enable security headers middleware.
    /// Default: true
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// X-Frame-Options header value.
    /// Default: "DENY"
    /// </summary>
    public string XFrameOptions { get; set; } = "DENY";

    /// <summary>
    /// Enable X-Content-Type-Options: nosniff header.
    /// Default: true
    /// </summary>
    public bool XContentTypeOptions { get; set; } = true;

    /// <summary>
    /// Enable X-XSS-Protection header (deprecated but still used by older browsers).
    /// Default: true
    /// </summary>
    public bool XXssProtection { get; set; } = true;

    /// <summary>
    /// Referrer-Policy header value.
    /// Default: "strict-origin-when-cross-origin"
    /// </summary>
    public string ReferrerPolicy { get; set; } = "strict-origin-when-cross-origin";

    /// <summary>
    /// Content-Security-Policy header value.
    /// Set to null to disable.
    /// </summary>
    public string? ContentSecurityPolicy { get; set; }

    /// <summary>
    /// Permissions-Policy header value.
    /// Set to null to disable.
    /// </summary>
    public string? PermissionsPolicy { get; set; }

    /// <summary>
    /// Enable Strict-Transport-Security (HSTS) header.
    /// Default: true
    /// </summary>
    public bool StrictTransportSecurity { get; set; } = true;

    /// <summary>
    /// HSTS max-age in seconds.
    /// Default: 31536000 (1 year)
    /// </summary>
    public int HstsMaxAgeSeconds { get; set; } = 31536000;

    /// <summary>
    /// Include subdomains in HSTS.
    /// Default: true
    /// </summary>
    public bool HstsIncludeSubdomains { get; set; } = true;

    /// <summary>
    /// Enable HSTS preload.
    /// Default: false
    /// </summary>
    public bool HstsPreload { get; set; } = false;

    /// <summary>
    /// Cross-Origin-Opener-Policy header value.
    /// Common values: "same-origin", "same-origin-allow-popups", "unsafe-none"
    /// </summary>
    public string? CrossOriginOpenerPolicy { get; set; } = "same-origin";

    /// <summary>
    /// Cross-Origin-Resource-Policy header value.
    /// Common values: "same-origin", "same-site", "cross-origin"
    /// </summary>
    public string? CrossOriginResourcePolicy { get; set; } = "same-origin";

    /// <summary>
    /// Cross-Origin-Embedder-Policy header value.
    /// Common values: "require-corp", "credentialless", "unsafe-none"
    /// </summary>
    public string? CrossOriginEmbedderPolicy { get; set; }
}

/// <summary>
/// Configuration for responsive image generation.
/// </summary>
public class ResponsiveImageOptions
{
    /// <summary>
    /// Default image widths for srcset generation.
    /// </summary>
    public List<int> DefaultWidths { get; set; } = [320, 640, 768, 1024, 1280, 1536, 1920];

    /// <summary>
    /// Default image quality (1-100).
    /// Default: 80
    /// </summary>
    public int DefaultQuality { get; set; } = 80;

    /// <summary>
    /// Default image format.
    /// Default: "webp"
    /// </summary>
    public string DefaultFormat { get; set; } = "webp";

    /// <summary>
    /// Whether to enable lazy loading by default.
    /// Default: true
    /// </summary>
    public bool EnableLazyLoading { get; set; } = true;

    /// <summary>
    /// Default sizes attribute for responsive images.
    /// </summary>
    public string DefaultSizes { get; set; } = "100vw";
}

/// <summary>
/// Configuration for ads.txt generation.
/// </summary>
public class AdsTxtOptions
{
    /// <summary>
    /// Enable ads.txt endpoint.
    /// Default: false
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Entries for ads.txt (one per line format: domain, publisher ID, relationship, certification).
    /// Example: "google.com, pub-0000000000000000, DIRECT, f08c47fec0942fa0"
    /// </summary>
    public List<string> Entries { get; set; } = [];
}

/// <summary>
/// Configuration for output cache policies.
/// </summary>
public class OutputCacheOptions
{
    /// <summary>
    /// Default cache expiration in minutes.
    /// Default: 10
    /// </summary>
    public int DefaultExpirationMinutes { get; set; } = 10;

    /// <summary>
    /// Static asset cache expiration in days.
    /// Default: 30
    /// </summary>
    public int StaticAssetExpirationDays { get; set; } = 30;
}

/// <summary>
/// Configuration for GEO (Generative Engine Optimization) services.
/// </summary>
public class GeoOptimizationOptions
{
    /// <summary>
    /// Enable GEO optimization features.
    /// Default: true
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Minimum GEO score for content to be considered AI-ready (0-100).
    /// Default: 60
    /// </summary>
    public int MinimumGeoScore { get; set; } = 60;

    /// <summary>
    /// Enable automatic GEO analysis on content save.
    /// Default: false
    /// </summary>
    public bool EnableAutoAnalysis { get; set; } = false;

    /// <summary>
    /// Enable GEO suggestions in admin UI.
    /// Default: true
    /// </summary>
    public bool EnableSuggestions { get; set; } = true;

    /// <summary>
    /// Enable Answer Engine optimization features.
    /// Default: true
    /// </summary>
    public bool EnableAnswerEngine { get; set; } = true;

    /// <summary>
    /// Auto-extract FAQs from content.
    /// Default: true
    /// </summary>
    public bool AutoExtractFaqs { get; set; } = true;

    /// <summary>
    /// Auto-generate HowTo schema for instructional content.
    /// Default: true
    /// </summary>
    public bool AutoGenerateHowTo { get; set; } = true;

    /// <summary>
    /// Enable speakable schema for voice search.
    /// Default: true
    /// </summary>
    public bool EnableSpeakable { get; set; } = true;

    /// <summary>
    /// CSS selectors for speakable content.
    /// </summary>
    public List<string> SpeakableSelectors { get; set; } = ["article", "h1", ".summary"];

    /// <summary>
    /// Maximum number of topics to extract per content item.
    /// Default: 5
    /// </summary>
    public int MaxTopics { get; set; } = 5;

    /// <summary>
    /// Maximum number of facts to extract per content item.
    /// Default: 10
    /// </summary>
    public int MaxFacts { get; set; } = 10;
}

/// <summary>
/// Configuration for SEO auditing services.
/// </summary>
public class SeoAuditingOptions
{
    /// <summary>
    /// Enable SEO auditing features.
    /// Default: true
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Minimum title length for SEO compliance.
    /// Default: 30
    /// </summary>
    public int MinTitleLength { get; set; } = 30;

    /// <summary>
    /// Maximum title length for SEO compliance.
    /// Default: 60
    /// </summary>
    public int MaxTitleLength { get; set; } = 60;

    /// <summary>
    /// Minimum meta description length.
    /// Default: 120
    /// </summary>
    public int MinMetaDescriptionLength { get; set; } = 120;

    /// <summary>
    /// Maximum meta description length.
    /// Default: 160
    /// </summary>
    public int MaxMetaDescriptionLength { get; set; } = 160;

    /// <summary>
    /// Minimum content word count for SEO compliance.
    /// Default: 300
    /// </summary>
    public int MinContentWordCount { get; set; } = 300;

    /// <summary>
    /// Maximum acceptable page load time in milliseconds.
    /// Default: 3000
    /// </summary>
    public int MaxLoadTimeMs { get; set; } = 3000;

    /// <summary>
    /// Enable GEO checks as part of SEO audit.
    /// Default: true
    /// </summary>
    public bool EnableGeoChecks { get; set; } = true;

    /// <summary>
    /// Enable scheduled audits.
    /// Default: false
    /// </summary>
    public bool EnableScheduledAudits { get; set; } = false;

    /// <summary>
    /// Scheduled audit interval in hours.
    /// Default: 24
    /// </summary>
    public int AuditIntervalHours { get; set; } = 24;
}

/// <summary>
/// Configuration for "Bring Your Own CDN" (BYOCDN) support.
/// Enables enterprise customers to use their own CDN with custom edge rules,
/// caching, routing, and security policies.
/// </summary>
public class CdnOptions
{
    /// <summary>
    /// Enable CDN support features.
    /// Default: false
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// CDN provider identifier (e.g., "cloudflare", "fastly", "akamai", "cloudfront", "azure-frontdoor", "custom").
    /// Used for provider-specific optimizations and header handling.
    /// </summary>
    public string? Provider { get; set; }

    /// <summary>
    /// The origin server base URL that the CDN will pull content from.
    /// Example: "https://origin.example.com"
    /// </summary>
    public string? OriginBaseUrl { get; set; }

    /// <summary>
    /// The CDN edge URL that end users access.
    /// Example: "https://cdn.example.com"
    /// </summary>
    public string? EdgeBaseUrl { get; set; }

    /// <summary>
    /// Enable CDN cache control headers middleware.
    /// Adds appropriate Cache-Control, Surrogate-Control, and CDN-specific headers.
    /// Default: true
    /// </summary>
    public bool EnableCacheHeaders { get; set; } = true;

    /// <summary>
    /// Enable stale-while-revalidate directive in Cache-Control.
    /// Default: true
    /// </summary>
    public bool EnableStaleWhileRevalidate { get; set; } = true;

    /// <summary>
    /// Stale-while-revalidate duration in seconds.
    /// Default: 86400 (24 hours)
    /// </summary>
    public int StaleWhileRevalidateSeconds { get; set; } = 86400;

    /// <summary>
    /// Enable stale-if-error directive for serving stale content on origin errors.
    /// Default: true
    /// </summary>
    public bool EnableStaleIfError { get; set; } = true;

    /// <summary>
    /// Stale-if-error duration in seconds.
    /// Default: 604800 (7 days)
    /// </summary>
    public int StaleIfErrorSeconds { get; set; } = 604800;

    /// <summary>
    /// Default cache TTL for dynamic pages in seconds.
    /// Default: 300 (5 minutes)
    /// </summary>
    public int DefaultPageTtlSeconds { get; set; } = 300;

    /// <summary>
    /// Cache TTL for static assets (images, CSS, JS) in seconds.
    /// Default: 31536000 (1 year)
    /// </summary>
    public int StaticAssetTtlSeconds { get; set; } = 31536000;

    /// <summary>
    /// Cache TTL for media library files in seconds.
    /// Default: 2592000 (30 days)
    /// </summary>
    public int MediaLibraryTtlSeconds { get; set; } = 2592000;

    /// <summary>
    /// Cache TTL for API responses in seconds.
    /// Default: 60 (1 minute)
    /// </summary>
    public int ApiTtlSeconds { get; set; } = 60;

    /// <summary>
    /// Enable Surrogate-Control header for CDN edge caching.
    /// Allows different cache durations at edge vs browser.
    /// Default: true
    /// </summary>
    public bool EnableSurrogateControl { get; set; } = true;

    /// <summary>
    /// Surrogate cache TTL at CDN edge in seconds (typically longer than browser cache).
    /// Default: 3600 (1 hour)
    /// </summary>
    public int SurrogateTtlSeconds { get; set; } = 3600;

    /// <summary>
    /// Enable CDN cache key customization hints via response headers.
    /// Default: true
    /// </summary>
    public bool EnableCacheKeyHints { get; set; } = true;

    /// <summary>
    /// Cache key vary headers to include.
    /// Default: ["Accept-Encoding", "Accept-Language"]
    /// </summary>
    public List<string> VaryHeaders { get; set; } = ["Accept-Encoding", "Accept-Language"];

    /// <summary>
    /// Cache key vary cookies to include (e.g., for A/B testing, personalization).
    /// Example: ["ab_test_variant", "user_segment"]
    /// </summary>
    public List<string> VaryCookies { get; set; } = [];

    /// <summary>
    /// Cache key vary query parameters to include.
    /// Default: empty (all query params included by default)
    /// </summary>
    public List<string> VaryQueryParams { get; set; } = [];

    /// <summary>
    /// Query parameters to ignore in cache key.
    /// Example: ["utm_source", "utm_medium", "utm_campaign", "fbclid", "gclid"]
    /// </summary>
    public List<string> IgnoreQueryParams { get; set; } = ["utm_source", "utm_medium", "utm_campaign", "utm_content", "utm_term", "fbclid", "gclid", "msclkid"];

    /// <summary>
    /// Enable purge/invalidation header hints for CDN integration.
    /// Default: true
    /// </summary>
    public bool EnablePurgeHints { get; set; } = true;

    /// <summary>
    /// Custom header name for cache tags/surrogate keys (used for targeted purging).
    /// Default: "Surrogate-Key" (Fastly convention, Cloudflare uses "Cache-Tag")
    /// </summary>
    public string CacheTagHeader { get; set; } = "Surrogate-Key";

    /// <summary>
    /// Enable cache tag generation for content items.
    /// Allows purging specific content when it changes.
    /// Default: true
    /// </summary>
    public bool EnableCacheTags { get; set; } = true;

    /// <summary>
    /// Maximum number of cache tags per response.
    /// Some CDNs have limits (e.g., Cloudflare: 30, Fastly: ~16KB total).
    /// Default: 25
    /// </summary>
    public int MaxCacheTags { get; set; } = 25;

    /// <summary>
    /// Prefix for generated cache tags.
    /// Helps avoid collisions in multi-site/multi-tenant scenarios.
    /// Example: "mysite_"
    /// </summary>
    public string? CacheTagPrefix { get; set; }

    /// <summary>
    /// Enable geo-location headers for edge personalization.
    /// Adds hints about country, region, city from CDN-provided headers.
    /// Default: false
    /// </summary>
    public bool EnableGeoHeaders { get; set; } = false;

    /// <summary>
    /// Geo-location header mappings for different CDN providers.
    /// Maps internal header names to CDN-specific headers.
    /// </summary>
    public CdnGeoHeaderMappings GeoHeaderMappings { get; set; } = new();

    /// <summary>
    /// Enable device detection headers from CDN.
    /// Default: false
    /// </summary>
    public bool EnableDeviceHeaders { get; set; } = false;

    /// <summary>
    /// Path prefixes that should bypass CDN caching (always pass-through).
    /// Example: ["/admin", "/api/auth", "/kentico"]
    /// Default: ["/admin", "/cmsmodules", "/getmedia"]
    /// </summary>
    public List<string> BypassPaths { get; set; } = ["/admin", "/cmsmodules"];

    /// <summary>
    /// Enable ESI (Edge Side Includes) support hints.
    /// Default: false
    /// </summary>
    public bool EnableEsiHints { get; set; } = false;

    /// <summary>
    /// ESI tag wrapper format.
    /// Default: "esi:include" (Akamai/Fastly style)
    /// </summary>
    public string EsiTagFormat { get; set; } = "esi:include";

    /// <summary>
    /// Enable request collapsing hint (coalescing multiple identical requests).
    /// Default: true
    /// </summary>
    public bool EnableRequestCollapsing { get; set; } = true;

    /// <summary>
    /// Enable prefetch hints for linked resources.
    /// Default: true
    /// </summary>
    public bool EnablePrefetchHints { get; set; } = true;

    /// <summary>
    /// Custom headers to add to all CDN-enabled responses.
    /// Example: {"X-CDN-Environment": "production"}
    /// </summary>
    public Dictionary<string, string> CustomHeaders { get; set; } = [];

    /// <summary>
    /// Enable debug headers (cache status, hit/miss, timing) in non-production.
    /// Default: true
    /// </summary>
    public bool EnableDebugHeaders { get; set; } = true;

    /// <summary>
    /// Header name to indicate cache status (used by debug headers).
    /// Default: "X-Cache-Status"
    /// </summary>
    public string CacheStatusHeader { get; set; } = "X-Cache-Status";
}

/// <summary>
/// Geo-location header mappings for different CDN providers.
/// </summary>
public class CdnGeoHeaderMappings
{
    /// <summary>
    /// Header containing country code (ISO 3166-1 alpha-2).
    /// Cloudflare: "CF-IPCountry", Fastly: "Fastly-Client-IP-Geo-Country"
    /// Default: "CF-IPCountry"
    /// </summary>
    public string CountryHeader { get; set; } = "CF-IPCountry";

    /// <summary>
    /// Header containing region/state code.
    /// Cloudflare: "CF-IPRegion", Fastly: "Fastly-Client-IP-Geo-Region"
    /// </summary>
    public string? RegionHeader { get; set; } = "CF-IPRegion";

    /// <summary>
    /// Header containing city name.
    /// Cloudflare: "CF-IPCity", Fastly: "Fastly-Client-IP-Geo-City"
    /// </summary>
    public string? CityHeader { get; set; } = "CF-IPCity";

    /// <summary>
    /// Header containing latitude.
    /// </summary>
    public string? LatitudeHeader { get; set; }

    /// <summary>
    /// Header containing longitude.
    /// </summary>
    public string? LongitudeHeader { get; set; }

    /// <summary>
    /// Header containing ASN (Autonomous System Number).
    /// </summary>
    public string? AsnHeader { get; set; }

    /// <summary>
    /// Header containing timezone.
    /// </summary>
    public string? TimezoneHeader { get; set; }
}
