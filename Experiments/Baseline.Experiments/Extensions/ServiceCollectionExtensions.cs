using Baseline.Experiments.Configuration;
using Baseline.Experiments.Infrastructure;
using Baseline.Experiments.Interfaces;
using Baseline.Experiments.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Baseline.Experiments.Extensions;

/// <summary>
/// Extension methods for registering Baseline.Experiments services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Baseline Experiments services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBaselineExperiments(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services.AddBaselineExperiments(options =>
        {
            configuration.GetSection("BaselineExperiments").Bind(options);
        });
    }

    /// <summary>
    /// Adds Baseline Experiments services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Action to configure options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBaselineExperiments(
        this IServiceCollection services,
        Action<BaselineExperimentsOptions>? configureOptions = null)
    {
        // Configure options
        var optionsBuilder = services.AddOptions<BaselineExperimentsOptions>();
        if (configureOptions != null)
        {
            optionsBuilder.Configure(configureOptions);
        }

        // Register module installer
        services.AddSingleton<IExperimentsModuleInstaller, ExperimentsModuleInstaller>();

        // Register core services — Scoped for request-level consistency
        services.AddSingleton<ITrafficSplitService, TrafficSplitService>();
        services.AddSingleton<IExperimentService, ExperimentService>();
        services.AddScoped<IVariantAssignmentService, CookieVariantAssignmentService>();
        services.AddScoped<IConversionTrackingService, ConversionTrackingService>();
        services.AddScoped<IStatisticsService, StatisticsService>();
        services.AddScoped<IXbkConversionBridgeService, XbkConversionBridgeService>();

        // Register specialized services
        services.AddScoped<IPageExperimentService, PageExperimentService>();
        services.AddScoped<IWidgetExperimentService, WidgetExperimentService>();
        services.AddScoped<IEmailExperimentService, EmailExperimentService>();

        // Ensure IHttpContextAccessor is registered
        services.AddHttpContextAccessor();

        // Add memory cache if not already registered
        services.AddMemoryCache();

        return services;
    }
}
