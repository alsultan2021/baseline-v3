using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods for configuring static file options with modern web optimizations.
/// </summary>
public static class StaticFileOptionsExtensions
{
    /// <summary>
    /// Gets optimized StaticFileOptions with modern MIME type mappings and caching headers.
    /// Includes support for WebP, AVIF, WOFF2, gzipped files, and versioned asset caching.
    /// </summary>
    /// <returns>Configured StaticFileOptions.</returns>
    /// <example>
    /// <code>
    /// app.UseStaticFiles(StaticFileOptionsExtensions.GetBaselineStaticFileOptions());
    /// </code>
    /// </example>
    public static StaticFileOptions GetBaselineStaticFileOptions()
    {
        var provider = new FileExtensionContentTypeProvider();

        // Add gzipped file type mappings
        provider.Mappings[".css.gz"] = "text/css";
        provider.Mappings[".js.gz"] = "application/javascript";
        provider.Mappings[".map.gz"] = "application/json";
        provider.Mappings[".json.gz"] = "application/json";
        provider.Mappings[".html.gz"] = "text/html";
        provider.Mappings[".svg.gz"] = "image/svg+xml";

        // Add Brotli file type mappings
        provider.Mappings[".css.br"] = "text/css";
        provider.Mappings[".js.br"] = "application/javascript";
        provider.Mappings[".json.br"] = "application/json";
        provider.Mappings[".html.br"] = "text/html";
        provider.Mappings[".svg.br"] = "image/svg+xml";

        // Add modern image formats
        provider.Mappings[".webp"] = "image/webp";
        provider.Mappings[".avif"] = "image/avif";
        provider.Mappings[".jxl"] = "image/jxl";

        // Add modern font formats
        provider.Mappings[".woff2"] = "font/woff2";
        provider.Mappings[".woff"] = "font/woff";

        // Add modern video formats
        provider.Mappings[".webm"] = "video/webm";
        provider.Mappings[".av1"] = "video/av1";

        // Add source maps
        provider.Mappings[".map"] = "application/json";

        return new StaticFileOptions
        {
            ContentTypeProvider = provider,
            OnPrepareResponse = ctx =>
            {
                var path = ctx.File.Name;
                var headers = ctx.Context.Response.Headers;

                // Content-Encoding for pre-compressed files
                if (path.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
                {
                    headers["Content-Encoding"] = "gzip";
                }
                else if (path.EndsWith(".br", StringComparison.OrdinalIgnoreCase))
                {
                    headers["Content-Encoding"] = "br";
                }

                // Aggressive caching for versioned files (immutable for 1 year)
                if (ctx.Context.Request.Query.ContainsKey("v"))
                {
                    headers["Cache-Control"] = "public,max-age=31536000,immutable";
                }
                else
                {
                    // Default 30-day caching for non-versioned static files
                    headers["Cache-Control"] = "public,max-age=2592000";
                }
            }
        };
    }

    /// <summary>
    /// Uses static files with Baseline's optimized configuration.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    public static IApplicationBuilder UseBaselineStaticFiles(this IApplicationBuilder app)
    {
        return app.UseStaticFiles(GetBaselineStaticFileOptions());
    }
}
