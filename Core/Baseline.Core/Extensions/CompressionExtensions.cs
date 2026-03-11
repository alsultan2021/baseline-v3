using System.IO.Compression;
using Baseline.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for configuring response and request compression.
    /// </summary>
    public static class CompressionExtensions
    {
        /// <summary>
        /// Adds response compression with Brotli and Gzip support, optimized for web applications.
        /// Also adds request decompression for handling compressed request bodies (.NET 7+).
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional action to configure compression options.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddBaselineCompression();
        /// // or with custom configuration
        /// services.AddBaselineCompression(options =>
        /// {
        ///     options.EnableForHttps = true;
        ///     options.BrotliLevel = CompressionLevel.Optimal;
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddBaselineCompression(
            this IServiceCollection services,
            Action<BaselineCompressionOptions>? configure = null)
        {
            var options = new BaselineCompressionOptions();
            configure?.Invoke(options);

            services.AddResponseCompression(responseOptions =>
            {
                responseOptions.EnableForHttps = options.EnableForHttps;
                responseOptions.Providers.Add<BrotliCompressionProvider>();
                responseOptions.Providers.Add<GzipCompressionProvider>();

                // Common MIME types for compression
                responseOptions.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(options.AdditionalMimeTypes);
            });

            services.Configure<BrotliCompressionProviderOptions>(brotliOptions =>
                brotliOptions.Level = options.BrotliLevel);

            services.Configure<GzipCompressionProviderOptions>(gzipOptions =>
                gzipOptions.Level = options.GzipLevel);

            // Request decompression for handling compressed POST/PUT bodies
            services.AddRequestDecompression();

            return services;
        }
    }
}

namespace Baseline.Core
{
    /// <summary>
    /// Options for configuring baseline compression.
    /// </summary>
    public class BaselineCompressionOptions
    {
        /// <summary>
        /// Enable compression over HTTPS. Default: true.
        /// Note: There are security considerations (BREACH attack) when enabling over HTTPS.
        /// </summary>
        public bool EnableForHttps { get; set; } = true;

        /// <summary>
        /// Brotli compression level. Default: Fastest for lower CPU usage.
        /// Use Optimal for better compression ratio at cost of CPU.
        /// </summary>
        public CompressionLevel BrotliLevel { get; set; } = CompressionLevel.Fastest;

        /// <summary>
        /// Gzip compression level. Default: SmallestSize for best compression.
        /// </summary>
        public CompressionLevel GzipLevel { get; set; } = CompressionLevel.SmallestSize;

        /// <summary>
        /// Additional MIME types to compress beyond the defaults.
        /// </summary>
        public string[] AdditionalMimeTypes { get; set; } =
        [
            "application/json",
            "application/xml",
            "text/plain",
            "text/csv",
            "application/javascript",
            "text/css",
            "image/svg+xml"
        ];
    }
}

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extension methods for using compression middleware.
    /// </summary>
    public static class CompressionApplicationBuilderExtensions
    {
        /// <summary>
        /// Uses response compression middleware. Should be called early in the pipeline.
        /// In development, compression is often skipped for easier debugging.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <param name="env">The host environment.</param>
        /// <param name="useInDevelopment">Whether to use compression in development. Default: false.</param>
        /// <returns>The application builder for chaining.</returns>
        public static IApplicationBuilder UseBaselineCompression(
            this IApplicationBuilder app,
            Microsoft.Extensions.Hosting.IHostEnvironment env,
            bool useInDevelopment = false)
        {
            // Request decompression is always useful
            app.UseRequestDecompression();

            // Response compression typically only in non-development
            if (useInDevelopment || !env.IsDevelopment())
            {
                app.UseResponseCompression();
            }

            return app;
        }
    }
}
