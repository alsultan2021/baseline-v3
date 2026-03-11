using System.Security.Cryptography;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Baseline.Core.Middleware;

/// <summary>
/// Middleware for adding security headers to responses.
/// Supports CSP nonce generation — use <c>{nonce}</c> placeholder in
/// <see cref="SecurityHeadersOptions.ContentSecurityPolicy"/> and retrieve the
/// per-request nonce via <see cref="CspNonceExtensions.GetCspNonce"/>.
/// </summary>
public class SecurityHeadersMiddleware(RequestDelegate next, IOptions<BaselineCoreOptions> options)
{
    private const string NonceContextKey = "Baseline.CspNonce";
    private const string NoncePlaceholder = "{nonce}";
    private readonly SecurityHeadersOptions _options = options.Value.SecurityHeaders;

    public async Task InvokeAsync(HttpContext context)
    {
        if (_options.Enabled)
        {
            AddSecurityHeaders(context);
        }

        await next(context);
    }

    private void AddSecurityHeaders(HttpContext context)
    {
        var headers = context.Response.Headers;

        // Content Security Policy (with optional nonce)
        if (!string.IsNullOrEmpty(_options.ContentSecurityPolicy))
        {
            var csp = _options.ContentSecurityPolicy;
            if (csp.Contains(NoncePlaceholder, StringComparison.Ordinal))
            {
                var nonce = GenerateNonce();
                context.Items[NonceContextKey] = nonce;
                csp = csp.Replace(NoncePlaceholder, $"'nonce-{nonce}'", StringComparison.Ordinal);
            }

            headers.TryAdd("Content-Security-Policy", csp);
        }

        // X-Content-Type-Options
        if (_options.XContentTypeOptions)
        {
            headers.TryAdd("X-Content-Type-Options", "nosniff");
        }

        // X-Frame-Options (skip for admin/cmsctx paths to allow Kentico FormBuilder iframes)
        if (!string.IsNullOrEmpty(_options.XFrameOptions) && !IsAdminPath(context.Request.Path))
        {
            headers.TryAdd("X-Frame-Options", _options.XFrameOptions);
        }

        // X-XSS-Protection (deprecated but still used by some older browsers)
        if (_options.XXssProtection)
        {
            headers.TryAdd("X-XSS-Protection", "1; mode=block");
        }

        // Referrer-Policy
        if (!string.IsNullOrEmpty(_options.ReferrerPolicy))
        {
            headers.TryAdd("Referrer-Policy", _options.ReferrerPolicy);
        }

        // Permissions-Policy
        if (!string.IsNullOrEmpty(_options.PermissionsPolicy))
        {
            headers.TryAdd("Permissions-Policy", _options.PermissionsPolicy);
        }

        // Strict-Transport-Security (HSTS)
        if (_options.StrictTransportSecurity)
        {
            var hsts = $"max-age={_options.HstsMaxAgeSeconds}";
            if (_options.HstsIncludeSubdomains)
            {
                hsts += "; includeSubDomains";
            }
            if (_options.HstsPreload)
            {
                hsts += "; preload";
            }
            headers.TryAdd("Strict-Transport-Security", hsts);
        }

        // Cross-Origin headers
        if (!string.IsNullOrEmpty(_options.CrossOriginOpenerPolicy))
        {
            headers.TryAdd("Cross-Origin-Opener-Policy", _options.CrossOriginOpenerPolicy);
        }

        if (!string.IsNullOrEmpty(_options.CrossOriginResourcePolicy))
        {
            headers.TryAdd("Cross-Origin-Resource-Policy", _options.CrossOriginResourcePolicy);
        }

        if (!string.IsNullOrEmpty(_options.CrossOriginEmbedderPolicy))
        {
            headers.TryAdd("Cross-Origin-Embedder-Policy", _options.CrossOriginEmbedderPolicy);
        }
    }

    private static string GenerateNonce()
    {
        Span<byte> bytes = stackalloc byte[16];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// Checks if the request path is an admin or CMS context path (FormBuilder, etc.)
    /// These paths need to allow iframe embedding for Xperience admin functionality.
    /// </summary>
    private static bool IsAdminPath(PathString path) =>
        path.StartsWithSegments("/admin", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWithSegments("/cmsctx", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Extension methods for retrieving the per-request CSP nonce.
/// </summary>
public static class CspNonceExtensions
{
    private const string NonceContextKey = "Baseline.CspNonce";

    /// <summary>
    /// Gets the CSP nonce generated for this request, or null if CSP nonce is not configured.
    /// Use in Razor views: <c>&lt;script nonce="@Context.GetCspNonce()"&gt;</c>
    /// </summary>
    public static string? GetCspNonce(this HttpContext context) =>
        context.Items.TryGetValue(NonceContextKey, out var nonce) ? nonce as string : null;
}

/// <summary>
/// Extension methods for adding security headers middleware.
/// </summary>
public static class SecurityHeadersMiddlewareExtensions
{
    /// <summary>
    /// Adds security headers middleware to the pipeline.
    /// </summary>
    public static IApplicationBuilder UseBaselineSecurityHeaders(this IApplicationBuilder app)
    {
        return app.UseMiddleware<SecurityHeadersMiddleware>();
    }
}
