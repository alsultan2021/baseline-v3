using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.WebEncoders;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for common MVC configuration patterns.
/// </summary>
public static class MvcConfigurationExtensions
{
    /// <summary>
    /// Configures feature folder view engine for organizing views by feature.
    /// Views are resolved from /Features/{Feature}/{View}.cshtml
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddFeatureFolderViewEngine();
    /// </code>
    /// </example>
    public static IServiceCollection AddFeatureFolderViewEngine(this IServiceCollection services)
    {
        services.Configure<Microsoft.AspNetCore.Mvc.Razor.RazorViewEngineOptions>(options =>
        {
            // Feature folder structure: /Features/{FeatureName}/{ViewName}.cshtml
            options.ViewLocationFormats.Insert(0, "/Features/{1}/{0}.cshtml");
            options.ViewLocationFormats.Insert(1, "/Features/{1}/{0}.vbhtml");
            options.ViewLocationFormats.Insert(2, "/Features/Shared/{0}.cshtml");

            // Area feature folder structure
            options.AreaViewLocationFormats.Insert(0, "/Areas/{2}/Features/{1}/{0}.cshtml");
            options.AreaViewLocationFormats.Insert(1, "/Areas/{2}/Features/Shared/{0}.cshtml");
        });

        return services;
    }

    /// <summary>
    /// Registers IUrlHelper as a scoped service.
    /// Fixes the common ASP.NET Core issue where IUrlHelper is not directly injectable.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddUrlHelper();
    /// // Then inject IUrlHelper in your services
    /// public class MyService(IUrlHelper urlHelper) { }
    /// </code>
    /// </example>
    public static IServiceCollection AddUrlHelper(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();

        services.AddScoped<IUrlHelper>(provider =>
        {
            var contextAccessor = provider.GetRequiredService<IHttpContextAccessor>();
            var httpContext = contextAccessor.HttpContext;

            if (httpContext == null)
            {
                // Return a minimal UrlHelper when no HttpContext available
                return new UrlHelper(new ActionContext());
            }

            var factory = provider.GetRequiredService<IUrlHelperFactory>();
            var actionContext = new ActionContext(
                httpContext,
                httpContext.GetRouteData(),
                new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());

            return factory.GetUrlHelper(actionContext);
        });

        return services;
    }

    /// <summary>
    /// Configures HTML encoder to support extended character sets.
    /// Prevents encoding of common characters like +, /, =, &amp;, ? which are needed in URLs and queries.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBaselineHtmlEncoder(this IServiceCollection services)
    {
        services.Configure<WebEncoderOptions>(options =>
        {
            options.TextEncoderSettings = new TextEncoderSettings(
                UnicodeRanges.BasicLatin,
                UnicodeRanges.Latin1Supplement,
                UnicodeRanges.LatinExtendedA,
                UnicodeRanges.LatinExtendedB);

            // Allow common URL and query string characters
            options.TextEncoderSettings.AllowCharacters('+', '/', '=', '&', '?');
        });

        return services;
    }

    /// <summary>
    /// Adds default output cache policies for common scenarios.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="defaultExpirationMinutes">Default cache expiration in minutes. Default: 10</param>
    /// <param name="staticAssetExpirationDays">Static asset cache expiration in days. Default: 30</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBaselineOutputCache(
        this IServiceCollection services,
        int defaultExpirationMinutes = 10,
        int staticAssetExpirationDays = 30)
    {
        services.AddOutputCache(options =>
        {
            // Base policy with default expiration
            options.AddBasePolicy(pb => pb
                .Expire(TimeSpan.FromMinutes(defaultExpirationMinutes))
                .Tag("default"));

            // Static assets policy with longer expiration and version-based vary
            options.AddPolicy("static-assets", pb => pb
                .Expire(TimeSpan.FromDays(staticAssetExpirationDays))
                .Tag("static")
                .SetVaryByQuery("v"));

            // No-cache policy for dynamic content
            options.AddPolicy("no-cache", pb => pb.NoCache());

            // API response caching with shorter duration
            options.AddPolicy("api", pb => pb
                .Expire(TimeSpan.FromMinutes(5))
                .Tag("api")
                .SetVaryByQuery("*"));
        });

        return services;
    }

    /// <summary>
    /// Configures standard route options with lowercase URLs.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="appendTrailingSlash">Whether to append trailing slash. Default: false</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBaselineRouteOptions(
        this IServiceCollection services,
        bool appendTrailingSlash = false)
    {
        services.Configure<RouteOptions>(options =>
        {
            options.LowercaseUrls = true;
            options.AppendTrailingSlash = appendTrailingSlash;
            options.LowercaseQueryStrings = false; // Must be proper case for token validation
        });

        return services;
    }
}
