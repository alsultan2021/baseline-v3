using Baseline.Ecommerce;
using Microsoft.Extensions.DependencyInjection;

namespace Ecommerce.Services;

/// <summary>
/// Extension methods to register v3 services with v2 method names
/// </summary>
public static class EcommerceServiceCollectionExtensions
{
    /// <summary>
    /// Adds Baseline Ecommerce with v2 method name for compatibility.
    /// </summary>
    public static IServiceCollection AddBaselineEcommerce(this IServiceCollection services)
    {
        return BaselineEcommerceServiceCollectionExtensions.AddBaselineEcommerce(services);
    }
}
