using Kentico.Xperience.MiniProfiler;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Baseline.Core;

/// <summary>
/// Extension methods for registering Baseline v3 services.
/// </summary>
public static class BaselineServiceCollectionExtensions
{
    /// <summary>
    /// Adds Baseline v3 Core services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action for Baseline options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// // Minimal setup with defaults
    /// services.AddBaselineCore();
    /// 
    /// // With custom configuration
    /// services.AddBaselineCore(options =>
    /// {
    ///     options.EnableStructuredData = true;
    ///     options.EnableResponsiveImages = true;
    ///     options.EnableFeatureFolderViewEngine = true;
    ///     options.LlmsTxt.SiteName = "My Site";
    ///     options.LlmsTxt.SiteDescription = "Description for AI crawlers.";
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddBaselineCore(
        this IServiceCollection services,
        Action<BaselineCoreOptions>? configure = null)
    {
        // Register options using the Options pattern
        services.AddOptions<BaselineCoreOptions>()
            .Configure(opt => configure?.Invoke(opt));

        // Build options locally for conditional DI registration decisions
        var options = new BaselineCoreOptions();
        configure?.Invoke(options);

        // Configure GEO and SEO audit options from BaselineCoreOptions
        // Uses IOptions<T> forwarding so configuration is resolved once at runtime
        services.AddOptions<Seo.GeoOptions>()
            .Configure<IOptions<BaselineCoreOptions>>((geoOpt, coreOpt) =>
            {
                var src = coreOpt.Value.Geo;
                geoOpt.MinimumGeoScore = src.MinimumGeoScore;
                geoOpt.EnableAutoAnalysis = src.EnableAutoAnalysis;
                geoOpt.EnableSuggestions = src.EnableSuggestions;
                geoOpt.MaxTopics = src.MaxTopics;
                geoOpt.MaxFacts = src.MaxFacts;
            });

        services.AddOptions<Seo.SeoAuditOptions>()
            .Configure<IOptions<BaselineCoreOptions>>((auditOpt, coreOpt) =>
            {
                var src = coreOpt.Value.SeoAudit;
                auditOpt.MinTitleLength = src.MinTitleLength;
                auditOpt.MaxTitleLength = src.MaxTitleLength;
                auditOpt.MinMetaDescriptionLength = src.MinMetaDescriptionLength;
                auditOpt.MaxMetaDescriptionLength = src.MaxMetaDescriptionLength;
                auditOpt.MinContentWordCount = src.MinContentWordCount;
                auditOpt.MaxLoadTimeMs = src.MaxLoadTimeMs;
                auditOpt.EnableGeoChecks = src.EnableGeoChecks;
            });

        // Configure LlmsTxt options — forward from BaselineCoreOptions at resolve time
        services.AddOptions<LlmsTxtOptions>()
            .Configure<IOptions<BaselineCoreOptions>>((llmsOpt, coreOpt) =>
            {
                var src = coreOpt.Value.LlmsTxt;
                llmsOpt.SiteName = src.SiteName;
                llmsOpt.SiteDescription = src.SiteDescription;
                llmsOpt.ContactEmail = src.ContactEmail;
                llmsOpt.ContactUrl = src.ContactUrl;
                llmsOpt.SupportUrl = src.SupportUrl;
                llmsOpt.Sections = src.Sections;
                llmsOpt.AutoDiscoverPages = src.AutoDiscoverPages;
                llmsOpt.IncludedContentTypes = src.IncludedContentTypes;
                llmsOpt.MaxPages = src.MaxPages;
                llmsOpt.Version = src.Version;
                llmsOpt.LastUpdated = src.LastUpdated;
                llmsOpt.PrimaryTopics = src.PrimaryTopics;
                llmsOpt.TargetAudience = src.TargetAudience;
                llmsOpt.EnableCapabilitiesSection = src.EnableCapabilitiesSection;
                llmsOpt.SupportedLanguages = src.SupportedLanguages;
                llmsOpt.ContentFormats = src.ContentFormats;
                llmsOpt.Features = src.Features;
                llmsOpt.EnableVectorIndex = src.EnableVectorIndex;
                llmsOpt.VectorIndexUrl = src.VectorIndexUrl;
                llmsOpt.VectorIndexFormat = src.VectorIndexFormat;
                llmsOpt.VectorDimensions = src.VectorDimensions;
                llmsOpt.EmbeddingModel = src.EmbeddingModel;
                llmsOpt.LicenseInfo = src.LicenseInfo;
                llmsOpt.AllowedUseCases = src.AllowedUseCases;
                llmsOpt.RestrictedUseCases = src.RestrictedUseCases;
            });

        // Register core services
        if (options.EnableStructuredData)
        {
            services.AddStructuredDataServices();
        }

        if (options.EnableResponsiveImages)
        {
            services.AddResponsiveImageServices();
        }

        // Register SEO endpoints
        services.AddSeoEndpointServices(options);

        // Register content retrieval wrapper
        services.AddContentRetrieverServices();

        // Register Core Options Provider for UI-configurable settings
        services.AddScoped<Services.ICoreOptionsProvider, Services.CoreOptionsProvider>();

        // Register MiniProfiler for development debugging
        if (options.EnableMiniProfiler)
        {
            services.AddKenticoMiniProfiler();
        }

        // Register MVC configuration extensions
        if (options.EnableFeatureFolderViewEngine)
        {
            services.AddFeatureFolderViewEngine();
        }

        if (options.EnableUrlHelper)
        {
            services.AddUrlHelper();
        }

        if (options.EnableHtmlEncoder)
        {
            services.AddBaselineHtmlEncoder();
        }

        if (options.EnableOutputCache)
        {
            services.AddBaselineOutputCache(
                options.OutputCache.DefaultExpirationMinutes,
                options.OutputCache.StaticAssetExpirationDays);
        }

        if (options.EnableCompression)
        {
            services.AddBaselineCompression(compressionOptions =>
            {
                compressionOptions.EnableForHttps = options.Compression.EnableForHttps;
                compressionOptions.BrotliLevel = options.Compression.BrotliLevel;
                compressionOptions.GzipLevel = options.Compression.GzipLevel;
                compressionOptions.AdditionalMimeTypes = options.Compression.AdditionalMimeTypes;
            });
        }

        if (options.EnableHealthChecks)
        {
            services.AddHealthChecks();
        }

        // Add MVC controllers for SEO endpoints
        services.AddControllers()
            .AddApplicationPart(typeof(BaselineServiceCollectionExtensions).Assembly);

        return services;
    }

    private static IServiceCollection AddStructuredDataServices(this IServiceCollection services)
    {
        services.AddScoped<IStructuredDataService, StructuredDataService>();
        services.AddScoped<IJsonLdGenerator, JsonLdGenerator>();
        services.AddScoped<Seo.IImageStructuredDataService, Seo.ImageStructuredDataService>();
        services.AddScoped<Seo.IRobotsMetaService, Seo.RobotsMetaService>();
        services.AddScoped<Seo.ISocialMediaImageService, Seo.SocialMediaImageService>();

        // GEO (Generative Engine Optimization) services
        services.AddScoped<Seo.IGeoOptimizationService, Seo.GeoOptimizationService>();
        services.AddScoped<Seo.IAnswerEngineService, Seo.AnswerEngineService>();
        services.AddScoped<Seo.ISeoAuditService, Seo.SeoAuditService>();

        return services;
    }

    private static IServiceCollection AddResponsiveImageServices(this IServiceCollection services)
    {
        services.TryAddSingleton(new ResponsiveImageOptions());
        services.AddScoped<IResponsiveImageService, ResponsiveImageService>();
        return services;
    }

    private static IServiceCollection AddSeoEndpointServices(
        this IServiceCollection services,
        BaselineCoreOptions options)
    {
        if (options.EnableRobotsTxt)
        {
            // Register RobotsTxtOptions from BaselineCoreOptions
            services.AddSingleton(options.RobotsTxt);
            services.AddScoped<IRobotsTxtService, RobotsTxtService>();
        }

        if (options.EnableLlmsTxt)
        {
            services.AddScoped<ILlmsTxtService, LlmsTxtService>();
        }

        if (options.EnableSecurityTxt && !string.IsNullOrEmpty(options.SecurityTxt.Contact))
        {
            services.AddScoped<ISecurityTxtService, SecurityTxtService>();
        }

        // Sitemap service — aggregates ISitemapEntryProvider instances from all modules
        services.TryAddScoped<ISitemapService, Seo.SitemapService>();

        return services;
    }

    private static IServiceCollection AddContentRetrieverServices(this IServiceCollection services)
    {
        services.AddScoped<IBaselineContentRetriever, BaselineContentRetriever>();
        services.AddScoped<Content.IContentQuery, Content.ContentQuery>();
        services.AddScoped<IPageIdentityFactory, PageIdentityFactory>();
        services.AddScoped<IMetaDataService, MetaDataService>();

        // HTML Minification service
        services.TryAddScoped<IHtmlMinificationService, HtmlMinificationService>();

        // Core utility services
        services.AddScoped<IUrlResolver, UrlResolver>();
        services.AddScoped<ILanguageService, LanguageService>();
        services.AddScoped<IPageContextRepository, PageContextRepository>();

        // Preview mode utilities
        services.AddScoped<IPreviewService, PreviewService>();

        return services;
    }
}
