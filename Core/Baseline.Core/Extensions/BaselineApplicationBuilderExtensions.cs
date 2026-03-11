using Baseline.Core.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Profiling;

namespace Baseline.Core;

/// <summary>
/// Extension methods for configuring Baseline Core middleware.
/// </summary>
public static class BaselineApplicationBuilderExtensions
{
    /// <summary>
    /// Adds Baseline Core middleware to the application pipeline.
    /// This includes security headers, CDN support, MiniProfiler (if enabled), and other Baseline-specific middleware.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    public static IApplicationBuilder UseBaselineCore(this IApplicationBuilder app)
    {
        // Get options to check if MiniProfiler is enabled
        var options = app.ApplicationServices.GetService<IOptions<BaselineCoreOptions>>()?.Value
            ?? new BaselineCoreOptions();

        // Add MiniProfiler middleware if enabled (development only)
        if (options.EnableMiniProfiler)
        {
            app.UseMiniProfiler();
        }

        // Add CDN middleware if enabled (should be early in pipeline for cache headers)
        if (options.Cdn.Enabled)
        {
            app.UseMiddleware<CdnMiddleware>();
        }

        // Add HTML minification middleware (checks IsEnabled at runtime per-request)
        app.UseMiddleware<HtmlMinificationMiddleware>();

        // Add security headers middleware
        app.UseMiddleware<SecurityHeadersMiddleware>();

        return app;
    }

    /// <summary>
    /// Adds CDN middleware separately for fine-grained control over pipeline order.
    /// Use this if you need CDN middleware at a specific point in your pipeline.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    public static IApplicationBuilder UseBaselineCdn(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CdnMiddleware>();
    }

    /// <summary>
    /// Maps Baseline Core endpoints (SEO endpoints are already attribute-routed).
    /// Use this if you need to manually configure endpoint routing.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The endpoint route builder for chaining.</returns>
    public static IEndpointRouteBuilder MapBaselineCoreEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // SEO endpoints are attribute-routed in SeoEndpointsController
        // Additional endpoint configuration can be added here if needed
        return endpoints;
    }
}
