using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace Baseline.SEO;

/// <summary>
/// Implementation of ISEOBuilder for configuring SEO services.
/// Follows the same pattern as AIBuilder and LuceneBuilder.
/// </summary>
public class SEOBuilder : ISEOBuilder
{
    private readonly List<Action<BaselineSEOOptions>> _configureActions = [];

    /// <inheritdoc />
    public IServiceCollection Services { get; }

    /// <inheritdoc />
    public bool IncludeDefaultAuditors { get; set; } = true;

    /// <summary>
    /// Creates a new SEO builder.
    /// </summary>
    /// <param name="services">The service collection.</param>
    public SEOBuilder(IServiceCollection services)
    {
        Services = services;
    }

    /// <inheritdoc />
    public ISEOBuilder RegisterGEOAnalyzer<TAnalyzer>() where TAnalyzer : class, IGEOAnalyzer
    {
        Services.AddTransient<IGEOAnalyzer, TAnalyzer>();
        SEOStrategyStorage.AddGEOAnalyzer<TAnalyzer>();
        return this;
    }

    /// <inheritdoc />
    public ISEOBuilder RegisterAuditor<TAuditor>() where TAuditor : class, ISEOAuditor
    {
        Services.AddTransient<ISEOAuditor, TAuditor>();
        SEOStrategyStorage.AddAuditor<TAuditor>();
        return this;
    }

    /// <inheritdoc />
    public ISEOBuilder RegisterSchemaGenerator<TGenerator>(string schemaType)
        where TGenerator : class, ISchemaGenerator
    {
        Services.AddTransient<ISchemaGenerator, TGenerator>();
        SEOStrategyStorage.AddSchemaGenerator<TGenerator>(schemaType);
        return this;
    }

    /// <inheritdoc />
    public ISEOBuilder RegisterLLMsSectionProvider<TProvider>()
        where TProvider : class, ILLMsSectionProvider
    {
        Services.AddTransient<ILLMsSectionProvider, TProvider>();
        SEOStrategyStorage.AddLLMsSectionProvider<TProvider>();
        return this;
    }

    /// <inheritdoc />
    public ISEOBuilder Configure(Action<BaselineSEOOptions> configure)
    {
        _configureActions.Add(configure);
        return this;
    }

    /// <summary>
    /// Applies all configuration actions to options.
    /// </summary>
    internal void ApplyConfiguration(BaselineSEOOptions options)
    {
        foreach (var action in _configureActions)
        {
            action(options);
        }
    }
}

/// <summary>
/// Static storage for registered SEO strategies and components.
/// Similar to StrategyStorage in Lucene integration.
/// </summary>
public static class SEOStrategyStorage
{
    private static readonly ConcurrentDictionary<string, Type> _geoAnalyzers = new();
    private static readonly ConcurrentDictionary<string, Type> _auditors = new();
    private static readonly ConcurrentDictionary<string, Type> _schemaGenerators = new();
    private static readonly ConcurrentDictionary<string, Type> _llmsSectionProviders = new();

    /// <summary>
    /// Gets registered GEO analyzers.
    /// </summary>
    public static IReadOnlyDictionary<string, Type> GEOAnalyzers => _geoAnalyzers;

    /// <summary>
    /// Gets registered SEO auditors.
    /// </summary>
    public static IReadOnlyDictionary<string, Type> Auditors => _auditors;

    /// <summary>
    /// Gets registered Schema.org generators.
    /// </summary>
    public static IReadOnlyDictionary<string, Type> SchemaGenerators => _schemaGenerators;

    /// <summary>
    /// Gets registered LLMs.txt section providers.
    /// </summary>
    public static IReadOnlyDictionary<string, Type> LLMsSectionProviders => _llmsSectionProviders;

    /// <summary>
    /// Adds a GEO analyzer.
    /// </summary>
    public static void AddGEOAnalyzer<TAnalyzer>() where TAnalyzer : class, IGEOAnalyzer
    {
        _geoAnalyzers[typeof(TAnalyzer).Name] = typeof(TAnalyzer);
    }

    /// <summary>
    /// Adds an SEO auditor.
    /// </summary>
    public static void AddAuditor<TAuditor>() where TAuditor : class, ISEOAuditor
    {
        _auditors[typeof(TAuditor).Name] = typeof(TAuditor);
    }

    /// <summary>
    /// Adds a Schema.org generator.
    /// </summary>
    public static void AddSchemaGenerator<TGenerator>(string schemaType)
        where TGenerator : class, ISchemaGenerator
    {
        _schemaGenerators[schemaType] = typeof(TGenerator);
    }

    /// <summary>
    /// Adds an LLMs.txt section provider.
    /// </summary>
    public static void AddLLMsSectionProvider<TProvider>()
        where TProvider : class, ILLMsSectionProvider
    {
        _llmsSectionProviders[typeof(TProvider).Name] = typeof(TProvider);
    }

    /// <summary>
    /// Clears all registered strategies (for testing).
    /// </summary>
    public static void Clear()
    {
        _geoAnalyzers.Clear();
        _auditors.Clear();
        _schemaGenerators.Clear();
        _llmsSectionProviders.Clear();
    }
}
