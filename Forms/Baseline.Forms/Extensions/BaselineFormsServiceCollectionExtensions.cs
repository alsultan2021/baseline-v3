using Baseline.Forms.Configuration;
using Baseline.Forms.Interfaces;
using Baseline.Forms.Localization.Adapters;
using Baseline.Forms.Localization.Configuration;
using Baseline.Forms.Localization.Interfaces;
using Baseline.Forms.Localization.Services;
using Baseline.Forms.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering Baseline Forms services.
/// </summary>
public static class BaselineFormsServiceCollectionExtensions
{
    /// <summary>
    /// Adds Baseline Forms services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Optional action to configure options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBaselineForms(
        this IServiceCollection services,
        Action<BaselineFormsOptions>? configureOptions = null)
    {
        if (configureOptions != null)
        {
            services.Configure<BaselineFormsOptions>(configureOptions);
        }

        // Register services
        services.AddScoped<IFormRetrievalService, FormRetrievalService>();
        services.AddScoped<IFormSubmissionService, FormSubmissionService>();
        services.AddScoped<IFormAutoresponderService, FormAutoresponderService>();

        return services;
    }

    /// <summary>
    /// Adds Baseline Forms services using configuration section.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="sectionName">The configuration section name. Default: "Baseline:Forms".</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBaselineForms(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = "Baseline:Forms")
    {
        services.Configure<BaselineFormsOptions>(configuration.GetSection(sectionName));

        // Register services
        services.AddScoped<IFormRetrievalService, FormRetrievalService>();
        services.AddScoped<IFormSubmissionService, FormSubmissionService>();
        services.AddScoped<IFormAutoresponderService, FormAutoresponderService>();

        return services;
    }

    /// <summary>
    /// Adds multilingual/localized forms support to Baseline Forms.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Optional action to configure localized forms options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// This method should be called after AddBaselineForms() as it depends on
    /// the base form services (IFormRetrievalService, IFormSubmissionService).
    /// You must also register implementations for IResourceStringProvider and ICultureProvider.
    /// </remarks>
    public static IServiceCollection AddLocalizedForms(
        this IServiceCollection services,
        Action<LocalizedFormsOptions>? configureOptions = null)
    {
        if (configureOptions != null)
        {
            services.Configure<LocalizedFormsOptions>(configureOptions);
        }

        // Register bridge adapters to Baseline.Localization (if not already registered)
        services.TryAddScoped<IResourceStringProvider, ContentHubResourceStringAdapter>();
        services.TryAddScoped<ICultureProvider, LanguageServiceCultureAdapter>();

        // Register localized form services
        services.AddScoped<ILocalizedFormService, LocalizedFormService>();
        services.AddScoped<ILocalizedValidationService, LocalizedValidationService>();
        services.AddScoped<ILocalizedFormSubmissionService, LocalizedFormSubmissionService>();
        services.AddScoped<IFormTranslationService, FormTranslationService>();

        return services;
    }

    /// <summary>
    /// Adds multilingual/localized forms support using configuration section.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="sectionName">The configuration section name. Default: "Baseline:Forms:Localization".</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddLocalizedForms(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = "Baseline:Forms:Localization")
    {
        services.Configure<LocalizedFormsOptions>(configuration.GetSection(sectionName));

        // Register bridge adapters to Baseline.Localization (if not already registered)
        services.TryAddScoped<IResourceStringProvider, ContentHubResourceStringAdapter>();
        services.TryAddScoped<ICultureProvider, LanguageServiceCultureAdapter>();

        // Register localized form services
        services.AddScoped<ILocalizedFormService, LocalizedFormService>();
        services.AddScoped<ILocalizedValidationService, LocalizedValidationService>();
        services.AddScoped<ILocalizedFormSubmissionService, LocalizedFormSubmissionService>();
        services.AddScoped<IFormTranslationService, FormTranslationService>();

        return services;
    }
}
