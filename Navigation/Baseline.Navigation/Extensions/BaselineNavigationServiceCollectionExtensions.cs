using Microsoft.Extensions.DependencyInjection;

namespace Baseline.Navigation;

/// <summary>
/// Extension methods for registering Baseline v3 Navigation services.
/// </summary>
public static class BaselineNavigationServiceCollectionExtensions
{
    /// <summary>
    /// Adds Baseline v3 Navigation services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action for Navigation options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// // Minimal setup with defaults
    /// services.AddBaselineNavigation();
    /// 
    /// // With custom configuration
    /// services.AddBaselineNavigation(options =>
    /// {
    ///     options.MaxNavigationDepth = 4;
    ///     options.Breadcrumbs.IncludeHome = true;
    ///     options.Breadcrumbs.GenerateStructuredData = true;
    ///     options.Sitemap.CacheDurationMinutes = 120;
    ///     options.EnableMegaMenus = true;
    ///     options.EnableDynamicNavigation = true;
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddBaselineNavigation(
        this IServiceCollection services,
        Action<BaselineNavigationOptions>? configure = null)
    {
        // Register options using the Options pattern
        services.AddOptions<BaselineNavigationOptions>()
            .Configure(opt => configure?.Invoke(opt));

        // Build options for conditional registration
        var options = new BaselineNavigationOptions();
        configure?.Invoke(options);

        // Core navigation services
        if (options.EnableBreadcrumbs)
        {
            services.AddScoped<IBreadcrumbService, BreadcrumbService>();
            services.AddScoped<IBreadcrumbJsonLdService, BreadcrumbJsonLdService>();
        }

        if (options.EnableSitemap)
        {
            services.AddScoped<ISitemapService, SitemapService>();
        }

        if (options.EnableMenus)
        {
            // Register MenuService as the implementation for both interfaces
            services.AddScoped<MenuService>();
            services.AddScoped<INavigationRepository>(sp => sp.GetRequiredService<MenuService>());
            services.AddScoped<IMenuService>(sp => sp.GetRequiredService<MenuService>());
        }

        // Always register URL service
        services.AddScoped<IPageUrlService, PageUrlService>();

        // Advanced navigation services
        if (options.EnableMegaMenus)
        {
            services.AddScoped<IMegaMenuService, MegaMenuService>();
        }

        if (options.EnableDynamicNavigation)
        {
            services.AddSingleton<IDynamicNavigationService, DynamicNavigationService>();
        }

        if (options.EnableNavigationCaching)
        {
            services.AddSingleton<INavigationCacheService, NavigationCacheService>();
        }

        if (options.EnableAccessibilityHelpers)
        {
            services.AddScoped<INavigationAccessibilityService, NavigationAccessibilityService>();
        }

        // Note: The consuming application must add this assembly as an application part
        // for ViewComponents to be discovered:
        // .AddApplicationPart(typeof(Baseline.Navigation.BaselineNavigationServiceCollectionExtensions).Assembly)

        return services;
    }

    /// <summary>
    /// Registers a dynamic navigation provider.
    /// </summary>
    /// <typeparam name="TProvider">The provider type implementing IDynamicNavigationProvider.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDynamicNavigationProvider<TProvider>(
        this IServiceCollection services) where TProvider : class, IDynamicNavigationProvider
    {
        services.AddSingleton<IDynamicNavigationProvider, TProvider>();
        return services;
    }
}
