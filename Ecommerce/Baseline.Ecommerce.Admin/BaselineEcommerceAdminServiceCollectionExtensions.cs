using Baseline.Ecommerce.Admin.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Baseline.Ecommerce.Admin;

/// <summary>
/// Extension methods for registering Baseline v3 Ecommerce Admin services.
/// </summary>
public static class BaselineEcommerceAdminServiceCollectionExtensions
{
    /// <summary>
    /// Adds Baseline v3 Ecommerce Admin services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBaselineEcommerceAdmin(this IServiceCollection services)
    {
        // Register default content language retriever for multilingual support
        services.AddTransient<IDefaultContentLanguageRetriever, DefaultContentLanguageRetriever>();

        // Register product metadata retriever for admin UI
        services.AddTransient<IProductMetadataRetriever, ProductMetadataRetriever>();

        return services;
    }
}
