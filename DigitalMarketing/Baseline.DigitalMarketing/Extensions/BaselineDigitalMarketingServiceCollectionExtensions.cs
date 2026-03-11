using Baseline.DigitalMarketing.Configuration;
using Baseline.DigitalMarketing.Interfaces;
using Baseline.DigitalMarketing.Services;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering Baseline Digital Marketing services.
/// </summary>
public static class BaselineDigitalMarketingServiceCollectionExtensions
{
    /// <summary>
    /// Adds Baseline Digital Marketing services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Optional action to configure options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBaselineDigitalMarketing(
        this IServiceCollection services,
        Action<BaselineDigitalMarketingOptions>? configureOptions = null)
    {
        var options = new BaselineDigitalMarketingOptions();
        configureOptions?.Invoke(options);
        
        services.Configure<BaselineDigitalMarketingOptions>(opt =>
        {
            opt.EnableContactTracking = options.EnableContactTracking;
            opt.EnableActivityLogging = options.EnableActivityLogging;
            opt.RequireConsentBeforeTracking = options.RequireConsentBeforeTracking;
            opt.TrackingConsentCodeName = options.TrackingConsentCodeName;
            opt.LogPageVisitActivities = options.LogPageVisitActivities;
            opt.ContactGroupCacheDurationMinutes = options.ContactGroupCacheDurationMinutes;
            opt.EnablePersonalizationDebugMode = options.EnablePersonalizationDebugMode;
            opt.CustomActivityTypes = options.CustomActivityTypes;
            opt.ContactFieldMappings = options.ContactFieldMappings;
            opt.ExcludedActivityTypes = options.ExcludedActivityTypes;
            opt.ExcludedPagePaths = options.ExcludedPagePaths;
        });

        // Register services
        services.AddScoped<IContactTrackingService, ContactTrackingService>();
        services.AddScoped<IActivityLoggingService, ActivityLoggingService>();
        services.AddScoped<IContactGroupService, ContactGroupService>();
        services.AddScoped<IPersonalizationService, PersonalizationService>();
        services.AddScoped<ICustomActivityTypeService, CustomActivityTypeService>();

        // CDP profile services (opt-in)
        if (options.EnableCdpProfileTracking)
        {
            services.AddScoped<IProfileTrackingService, ProfileTrackingService>();
            services.AddScoped<IProfileConsentService, ProfileConsentService>();
        }

        return services;
    }

    /// <summary>
    /// Adds Baseline Digital Marketing services using configuration section.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="sectionName">The configuration section name. Default: "Baseline:DigitalMarketing".</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBaselineDigitalMarketing(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = "Baseline:DigitalMarketing")
    {
        services.Configure<BaselineDigitalMarketingOptions>(configuration.GetSection(sectionName));

        // Bind to check EnableCdpProfileTracking flag
        var options = new BaselineDigitalMarketingOptions();
        configuration.GetSection(sectionName).Bind(options);

        // Register services
        services.AddScoped<IContactTrackingService, ContactTrackingService>();
        services.AddScoped<IActivityLoggingService, ActivityLoggingService>();
        services.AddScoped<IContactGroupService, ContactGroupService>();
        services.AddScoped<IPersonalizationService, PersonalizationService>();
        services.AddScoped<ICustomActivityTypeService, CustomActivityTypeService>();

        // CDP profile services (opt-in)
        if (options.EnableCdpProfileTracking)
        {
            services.AddScoped<IProfileTrackingService, ProfileTrackingService>();
            services.AddScoped<IProfileConsentService, ProfileConsentService>();
        }

        return services;
    }
}
