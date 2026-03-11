using Baseline.DataProtection.Configuration;
using Baseline.DataProtection.Interfaces;
using Baseline.DataProtection.Services;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering Baseline Data Protection services.
/// </summary>
public static class BaselineDataProtectionServiceCollectionExtensions
{
    /// <summary>
    /// Adds Baseline Data Protection services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Optional action to configure options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBaselineDataProtection(
        this IServiceCollection services,
        Action<BaselineDataProtectionOptions>? configureOptions = null)
    {
        var options = new BaselineDataProtectionOptions();
        configureOptions?.Invoke(options);

        services.Configure<BaselineDataProtectionOptions>(opt =>
        {
            opt.RequireConsentBeforeTracking = options.RequireConsentBeforeTracking;
            opt.BannerPosition = options.BannerPosition;
            opt.ConsentCookieExpirationDays = options.ConsentCookieExpirationDays;
            opt.ConsentCookieName = options.ConsentCookieName;
            opt.RequiredConsentsForTracking = options.RequiredConsentsForTracking;
            opt.ShowConsentBanner = options.ShowConsentBanner;
            opt.ShowRejectButton = options.ShowRejectButton;
            opt.ShowCustomizeButton = options.ShowCustomizeButton;
            opt.PrivacyPolicyUrl = options.PrivacyPolicyUrl;
            opt.CookiePolicyUrl = options.CookiePolicyUrl;
            opt.EnableDataExport = options.EnableDataExport;
            opt.EnableDataErasure = options.EnableDataErasure;
            opt.RequireEmailVerification = options.RequireEmailVerification;
            opt.DataErasureDeadlineDays = options.DataErasureDeadlineDays;
            opt.DataExportFormats = options.DataExportFormats;
            opt.BannerText = options.BannerText;
            opt.CookieCategories = options.CookieCategories;
        });

        // Register core services
        services.AddScoped<IConsentService, ConsentService>();
        services.AddScoped<ICookieConsentService, CookieConsentService>();
        services.AddScoped<IDataSubjectRightsService, DataSubjectRightsService>();

        // Register GDPR compliance services
        services.AddScoped<IGdprComplianceService, GdprComplianceService>();
        services.AddScoped<IDataRetentionService, DataRetentionService>();
        services.AddScoped<IDataAnonymizationService, DataAnonymizationService>();

        return services;
    }

    /// <summary>
    /// Adds Baseline Data Protection services using configuration section.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="sectionName">The configuration section name. Default: "Baseline:DataProtection".</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBaselineDataProtection(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = "Baseline:DataProtection")
    {
        services.Configure<BaselineDataProtectionOptions>(configuration.GetSection(sectionName));

        // Register core services
        services.AddScoped<IConsentService, ConsentService>();
        services.AddScoped<ICookieConsentService, CookieConsentService>();
        services.AddScoped<IDataSubjectRightsService, DataSubjectRightsService>();

        // Register GDPR compliance services
        services.AddScoped<IGdprComplianceService, GdprComplianceService>();
        services.AddScoped<IDataRetentionService, DataRetentionService>();
        services.AddScoped<IDataAnonymizationService, DataAnonymizationService>();

        return services;
    }
}
