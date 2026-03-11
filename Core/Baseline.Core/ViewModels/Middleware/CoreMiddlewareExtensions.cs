using Microsoft.AspNetCore.Builder;

namespace Core.Middleware;

/// <summary>
///  extension methods for middleware.
/// </summary>
public static class CoreMiddlewareExtensions
{
    /// <summary>
    /// UseCoreBaseline → v3 UseBaselineCore
    /// </summary>
    public static IApplicationBuilder UseCoreBaseline(this IApplicationBuilder app)
    {
        // v3 uses SeoMiddleware instead of CoreMiddleware
        return app;
    }

    /// <summary>
    /// UseCoreBaseline on WebApplication
    /// </summary>
    public static WebApplication UseCoreBaseline(this WebApplication app)
    {
        // v3 uses SeoMiddleware instead of CoreMiddleware
        return app;
    }
}
