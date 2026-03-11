using Microsoft.AspNetCore.Builder;

namespace Account.Middleware;

/// <summary>
///  extension methods for middleware.
/// </summary>
public static class AccountMiddlewareExtensions
{
    /// <summary>
    /// UseAccountBaseline
    /// </summary>
    public static IApplicationBuilder UseAccountBaseline(this IApplicationBuilder app) => app;

    /// <summary>
    /// UseAccountBaseline on WebApplication
    /// </summary>
    public static WebApplication UseAccountBaseline(this WebApplication app) => app;
}
