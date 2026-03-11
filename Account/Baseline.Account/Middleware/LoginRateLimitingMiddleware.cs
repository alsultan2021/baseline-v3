using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baseline.Account;

/// <summary>
/// Middleware to protect login endpoints from brute-force attacks.
/// Uses login audit trail to detect suspicious activity patterns.
/// </summary>
public class LoginRateLimitingMiddleware(
    RequestDelegate next,
    ILogger<LoginRateLimitingMiddleware> logger)
{
    // Endpoints to protect
    private static readonly HashSet<string> ProtectedPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/account/login",
        "/account/signin",
        "/api/account/login",
        "/api/account/signin",
        "/api/auth/login"
    };

    public async Task InvokeAsync(HttpContext context, IServiceProvider serviceProvider)
    {
        var path = context.Request.Path.Value ?? "";

        // Only check POST requests to login endpoints
        if (!context.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase) ||
            !IsProtectedPath(path))
        {
            await next(context);
            return;
        }

        var options = serviceProvider.GetService<IOptions<LoginAuditOptions>>();
        if (options?.Value.Enabled != true)
        {
            await next(context);
            return;
        }

        var auditService = serviceProvider.GetService<ILoginAuditService>();
        if (auditService == null)
        {
            await next(context);
            return;
        }

        var ipAddress = GetClientIpAddress(context);
        if (string.IsNullOrEmpty(ipAddress))
        {
            await next(context);
            return;
        }

        try
        {
            var hasSuspiciousActivity = await auditService.HasSuspiciousActivityAsync(
                ipAddress,
                options.Value.SuspiciousActivityWindow,
                options.Value.SuspiciousActivityThreshold);

            if (hasSuspiciousActivity)
            {
                logger.LogWarning(
                    "Blocked login attempt from {IpAddress} due to suspicious activity",
                    ipAddress);

                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.Headers.Append("Retry-After", "60");
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Too many failed login attempts. Please try again later.",
                    retryAfterSeconds = 60
                });
                return;
            }
        }
        catch (Exception ex)
        {
            // Don't block login if audit check fails
            logger.LogError(ex, "Failed to check suspicious activity for {IpAddress}", ipAddress);
        }

        await next(context);
    }

    private static bool IsProtectedPath(string path)
    {
        return ProtectedPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));
    }

    private static string? GetClientIpAddress(HttpContext context)
    {
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
            return forwardedFor.Split(',').First().Trim();

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
            return realIp;

        return context.Connection.RemoteIpAddress?.ToString();
    }
}

/// <summary>
/// Extension methods for login rate limiting middleware.
/// </summary>
public static class LoginRateLimitingMiddlewareExtensions
{
    /// <summary>
    /// Adds login rate limiting middleware to protect against brute-force attacks.
    /// Should be added before authentication middleware.
    /// </summary>
    public static IApplicationBuilder UseLoginRateLimiting(this IApplicationBuilder app)
    {
        return app.UseMiddleware<LoginRateLimitingMiddleware>();
    }

    /// <summary>
    /// Adds custom protected paths to the rate limiting middleware.
    /// </summary>
    public static IServiceCollection AddLoginRateLimiting(
        this IServiceCollection services,
        Action<LoginRateLimitingOptions>? configure = null)
    {
        services.AddOptions<LoginRateLimitingOptions>()
            .Configure(opt => configure?.Invoke(opt));

        return services;
    }
}

/// <summary>
/// Options for login rate limiting middleware.
/// </summary>
public class LoginRateLimitingOptions
{
    /// <summary>
    /// Additional paths to protect (in addition to default login paths).
    /// </summary>
    public List<string> AdditionalProtectedPaths { get; set; } = [];

    /// <summary>
    /// Message to show when rate limited.
    /// </summary>
    public string RateLimitMessage { get; set; } = "Too many failed login attempts. Please try again later.";

    /// <summary>
    /// Seconds to wait before retry.
    /// </summary>
    public int RetryAfterSeconds { get; set; } = 60;
}
