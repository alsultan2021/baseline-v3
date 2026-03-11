using Baseline.Localization.Events;
using Baseline.Localization.Infrastructure;
using Baseline.Localization.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Localization;

namespace Baseline.Localization;

/// <summary>
/// Extension methods for registering Baseline v3 Localization services.
/// </summary>
public static class BaselineLocalizationServiceCollectionExtensions
{
    /// <summary>
    /// Adds Baseline v3 Localization services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action for Localization options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// // Recommended: auto-detect cultures from XbK ContentLanguageInfo
    /// services.AddBaselineLocalization(options =>
    /// {
    ///     options.AutoDetectCulturesFromXbK = true; // default
    ///     options.EnableUrlCultureProvider = true;
    ///     options.EnableContentHubLocalizer = true;
    ///     options.EnableHreflangLinks = true;
    /// });
    ///
    /// // Or manually specify cultures:
    /// services.AddBaselineLocalization(options =>
    /// {
    ///     options.AutoDetectCulturesFromXbK = false;
    ///     options.DefaultCulture = "en-US";
    ///     options.SupportedCultures =
    ///     [
    ///         new("en-US", "English"),
    ///         new("fr-FR", "Français")
    ///     ];
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddBaselineLocalization(
        this IServiceCollection services,
        Action<BaselineLocalizationOptions>? configure = null)
    {
        // Register options using the Options pattern only
        services.AddOptions<BaselineLocalizationOptions>()
            .Configure(opt => configure?.Invoke(opt));

        // Also register a singleton for direct injection (consistent source)
        var options = new BaselineLocalizationOptions();
        configure?.Invoke(options);
        services.AddSingleton(options);

        // Core language service - integrates with XbK's IPreferredLanguageRetriever
        services.AddScoped<ILanguageService, LanguageService>();

        // Culture service - manages current/supported/default cultures
        services.AddScoped<ICultureService, CultureService>();

        // Localized URL service - URL resolution per language
        services.AddScoped<ILocalizedUrlService, LocalizedUrlService>();

        // Localized category repository for taxonomies
        services.AddScoped<ILocalizedCategoryRepository, LocalizedCategoryRepository>();

        // Text direction service for RTL/LTR support
        services.AddScoped<ITextDirectionService, TextDirectionService>();

        // Configure ASP.NET Core localization
        services.AddLocalization();

        // Content Hub-based string localizer (if enabled)
        if (options.EnableContentHubLocalizer)
        {
            var contentHubOptions = new ContentHubLocalizationOptions
            {
                CacheDurationMinutes = options.CacheDurationMinutes,
                UseFallbackLanguage = options.EnableFallbackToDefault
            };
            services.AddSingleton(contentHubOptions);
            services.AddScoped<IContentHubStringLocalizer, ContentHubStringLocalizer>();
            services.TryAddScoped<IStringLocalizerFactory, ContentHubStringLocalizerFactory>();

            // Register string-based services that depend on ContentHubStringLocalizer
            services.AddScoped<ILocalizationService, LocalizationService>();
            services.AddScoped<IResourceStringService, ResourceStringService>();
        }

        // Hreflang SEO service (if enabled)
        if (options.EnableHreflangLinks)
        {
            var hreflangOptions = new HreflangOptions();
            services.AddSingleton(hreflangOptions);
            services.AddScoped<IHreflangService, HreflangService>();
        }

        // AIRA translation service
        if (options.EnableAIRATranslation)
        {
            services.AddScoped<IAIRATranslationService, AIRATranslationService>();
        }

        // Translation workflow webhook service (default: logging-only)
        if (options.EnableTranslationWorkflow)
        {
            services.TryAddSingleton<ITranslationWebhookService, LoggingTranslationWebhookService>();
        }

        // Translation coverage service + installer (for admin dashboard)
        services.AddSingleton<ITranslationCoverageInstaller, TranslationCoverageInstaller>();
        services.AddScoped<ITranslationCoverageService, TranslationCoverageService>();

        return services;
    }

    /// <summary>
    /// Adds advanced localization services with full configuration.
    /// </summary>
    public static IServiceCollection AddBaselineLocalizationAdvanced(
        this IServiceCollection services,
        Action<BaselineLocalizationOptions>? localizationConfigure = null,
        Action<ContentHubLocalizationOptions>? contentHubConfigure = null,
        Action<HreflangOptions>? hreflangConfigure = null)
    {
        // Base localization
        services.AddBaselineLocalization(localizationConfigure);

        // Custom Content Hub options
        if (contentHubConfigure is not null)
        {
            var contentHubOptions = new ContentHubLocalizationOptions();
            contentHubConfigure(contentHubOptions);
            services.AddSingleton(contentHubOptions);
            services.AddScoped<IContentHubStringLocalizer, ContentHubStringLocalizer>();
            services.TryAddScoped<IStringLocalizerFactory, ContentHubStringLocalizerFactory>();
        }

        // Custom Hreflang options
        if (hreflangConfigure is not null)
        {
            var hreflangOptions = new HreflangOptions();
            hreflangConfigure(hreflangOptions);
            services.AddSingleton(hreflangOptions);
            services.AddScoped<IHreflangService, HreflangService>();
        }

        return services;
    }
}
