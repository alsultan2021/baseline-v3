using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Baseline.SEO;

/// <summary>
/// Extension methods for registering Baseline v3 SEO services.
/// </summary>
public static class BaselineSEOServiceCollectionExtensions
{
    /// <summary>
    /// Adds Baseline v3 SEO services and returns an <see cref="ISEOBuilder"/>
    /// for further configuration (analyzers, auditors, schema generators, etc.).
    /// <para>
    /// Default no-op implementations are registered for all service interfaces.
    /// Override any service by registering your own implementation before or after
    /// calling this method (last registration wins for <c>TryAdd</c> — register
    /// custom implementations <b>after</b> calling <c>AddBaselineSEO()</c>, or use
    /// <c>services.AddScoped&lt;IXxxService, MyService&gt;()</c> which replaces).
    /// </para>
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action for SEO options.</param>
    /// <returns>An <see cref="ISEOBuilder"/> for chaining registrations.</returns>
    /// <example>
    /// <code>
    /// services.AddBaselineSEO(options =>
    /// {
    ///     options.LLMs.SiteName = "My Site";
    ///     options.LLMs.SiteDescription = "Description";
    /// })
    /// .RegisterGEOAnalyzer&lt;MyGEOAnalyzer&gt;()
    /// .RegisterSchemaGenerator&lt;MySchemaGen&gt;("Article");
    ///
    /// // Override the no-op LLMs service with a real implementation
    /// services.AddScoped&lt;ILLMsService, MyLLMsService&gt;();
    /// </code>
    /// </example>
    public static ISEOBuilder AddBaselineSEO(
        this IServiceCollection services,
        Action<BaselineSEOOptions>? configure = null)
    {
        // Register options
        services.AddOptions<BaselineSEOOptions>()
            .Configure(opt => configure?.Invoke(opt));

        // Register default no-op implementations (TryAdd = won't replace existing registrations)
        services.TryAddScoped<IGEOOptimizationService, NoOpGEOOptimizationService>();
        services.TryAddScoped<IAnswerEngineService, NoOpAnswerEngineService>();
        services.TryAddScoped<ISEOAuditService, NoOpSEOAuditService>();
        services.TryAddScoped<ILLMsService, NoOpLLMsService>();

        // Return builder for fluent registration
        return new SEOBuilder(services);
    }
}
