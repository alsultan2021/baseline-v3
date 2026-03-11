using Baseline.Forms.Configuration;
using Baseline.Forms.Localization.Configuration;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering Baseline.Forms.RCL services.
/// </summary>
public static class FormsRclServiceCollectionExtensions
{
    /// <summary>
    /// Adds both the core Baseline Forms services and the localized forms layer
    /// required by <c>DynamicForm</c> and <c>FormField</c> Blazor components.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureForms">Optional action to configure core forms options.</param>
    /// <param name="configureLocalized">Optional action to configure localized forms options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBaselineFormsRcl(
        this IServiceCollection services,
        Action<BaselineFormsOptions>? configureForms = null,
        Action<LocalizedFormsOptions>? configureLocalized = null)
    {
        services.AddBaselineForms(configureForms);
        services.AddLocalizedForms(configureLocalized);
        return services;
    }

    /// <summary>
    /// Adds both the core Baseline Forms services and the localized forms layer
    /// using configuration sections.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="formsSectionName">Core forms section. Default: "Baseline:Forms".</param>
    /// <param name="localizedSectionName">Localized forms section. Default: "Baseline:Forms:Localization".</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBaselineFormsRcl(
        this IServiceCollection services,
        IConfiguration configuration,
        string formsSectionName = "Baseline:Forms",
        string localizedSectionName = "Baseline:Forms:Localization")
    {
        services.AddBaselineForms(configuration, formsSectionName);
        services.AddLocalizedForms(configuration, localizedSectionName);
        return services;
    }
}
