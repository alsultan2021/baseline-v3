using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Baseline.Core.Middleware;

/// <summary>
/// Middleware that minifies HTML responses by collapsing extra whitespace.
/// Only runs when <see cref="BaselineCoreOptions.EnableHtmlMinification"/> is true.
/// Preserves whitespace inside pre, code, script, textarea, and style elements.
/// </summary>
public class HtmlMinificationMiddleware(RequestDelegate next)
{
    private const string HtmlContentType = "text/html";

    public async Task InvokeAsync(HttpContext context)
    {
        var minificationService = context.RequestServices.GetService<IHtmlMinificationService>();

        // Skip if minification is disabled or service unavailable
        if (minificationService is null || !minificationService.IsEnabled)
        {
            await next(context);
            return;
        }

        // Skip for non-GET requests, static files, or API endpoints
        if (!ShouldMinify(context.Request))
        {
            await next(context);
            return;
        }

        // Buffer the response
        var originalBodyStream = context.Response.Body;
        using var newBodyStream = new MemoryStream();
        context.Response.Body = newBodyStream;

        await next(context);

        // Only minify HTML responses that are not already compressed
        var contentType = context.Response.ContentType?.ToLowerInvariant() ?? "";
        var contentEncoding = context.Response.Headers.ContentEncoding.ToString();
        if (contentType.Contains(HtmlContentType, StringComparison.Ordinal) &&
            context.Response.StatusCode == 200 &&
            string.IsNullOrEmpty(contentEncoding))
        {
            newBodyStream.Seek(0, SeekOrigin.Begin);
            var originalHtml = await new StreamReader(newBodyStream).ReadToEndAsync();

            if (!string.IsNullOrEmpty(originalHtml))
            {
                var minifiedHtml = minificationService.Minify(originalHtml);
                var minifiedBytes = System.Text.Encoding.UTF8.GetBytes(minifiedHtml);

                context.Response.Body = originalBodyStream;
                context.Response.ContentLength = minifiedBytes.Length;
                await context.Response.Body.WriteAsync(minifiedBytes);
                return;
            }
        }

        // If not minified, write original response
        newBodyStream.Seek(0, SeekOrigin.Begin);
        await newBodyStream.CopyToAsync(originalBodyStream);
        context.Response.Body = originalBodyStream;
    }

    private static bool ShouldMinify(HttpRequest request)
    {
        // Only GET requests
        if (!HttpMethods.IsGet(request.Method))
            return false;

        // Skip API endpoints
        var path = request.Path.Value?.ToLowerInvariant() ?? "";
        if (path.StartsWith("/api/", StringComparison.Ordinal) ||
            path.StartsWith("/_", StringComparison.Ordinal) ||
            path.StartsWith("/admin/", StringComparison.Ordinal))
            return false;

        // Skip static files
        if (path.EndsWith(".js", StringComparison.Ordinal) ||
            path.EndsWith(".css", StringComparison.Ordinal) ||
            path.EndsWith(".json", StringComparison.Ordinal) ||
            path.EndsWith(".map", StringComparison.Ordinal) ||
            path.EndsWith(".ico", StringComparison.Ordinal) ||
            path.EndsWith(".svg", StringComparison.Ordinal) ||
            path.EndsWith(".png", StringComparison.Ordinal) ||
            path.EndsWith(".jpg", StringComparison.Ordinal) ||
            path.EndsWith(".webp", StringComparison.Ordinal))
            return false;

        return true;
    }
}

/// <summary>
/// Extension methods for HTML minification middleware.
/// </summary>
public static class HtmlMinificationMiddlewareExtensions
{
    /// <summary>
    /// Adds HTML minification middleware to the pipeline.
    /// Call this early in the pipeline (after exception handlers, before MVC).
    /// </summary>
    public static IApplicationBuilder UseHtmlMinification(this IApplicationBuilder app)
    {
        return app.UseMiddleware<HtmlMinificationMiddleware>();
    }
}
