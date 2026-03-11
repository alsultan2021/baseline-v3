using Microsoft.Extensions.DependencyInjection;

namespace Baseline.TabbedPages;

/// <summary>
/// Extension methods for registering Baseline v3 TabbedPages services.
/// </summary>
public static class BaselineTabbedPagesServiceCollectionExtensions
{
    /// <summary>
    /// Adds Baseline v3 TabbedPages services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action for TabbedPages options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddBaselineTabbedPages(options =>
    /// {
    ///     options.Rendering.TabStyle = "horizontal";
    ///     options.Rendering.EnableAnimation = true;
    ///     options.Behavior.PersistInUrl = true;
    ///     options.Seo.RenderAllContentForSeo = true;
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddBaselineTabbedPages(
        this IServiceCollection services,
        Action<BaselineTabbedPagesOptions>? configure = null)
    {
        // Register options using the Options pattern
        services.AddOptions<BaselineTabbedPagesOptions>()
            .Configure(opt => configure?.Invoke(opt));

        // Register tabbed page services
        services.AddScoped<ITabbedPageService, TabbedPageService>();
        services.AddScoped<ITabRenderingService, TabRenderingService>();
        services.AddScoped<ITabSeoService, TabSeoService>();

        return services;
    }
}
