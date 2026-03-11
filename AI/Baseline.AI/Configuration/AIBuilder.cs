using Microsoft.Extensions.DependencyInjection;

namespace Baseline.AI;

/// <summary>
/// Internal implementation of IAIBuilder.
/// </summary>
internal sealed class AIBuilder : IAIBuilder
{
    private readonly IServiceCollection _services;

    /// <inheritdoc />
    public bool IncludeDefaultStrategy { get; set; } = true;

    public AIBuilder(IServiceCollection services)
    {
        _services = services;
    }

    /// <inheritdoc />
    public IAIBuilder RegisterStrategy<TStrategy>(string strategyName)
        where TStrategy : class, IAIIndexingStrategy
    {
        AIStrategyStorage.AddStrategy<TStrategy>(strategyName);
        _services.AddTransient<TStrategy>();
        return this;
    }

    /// <inheritdoc />
    public IAIBuilder RegisterProvider<TProvider>()
        where TProvider : class, IAIProvider
    {
        _services.AddSingleton<IAIProvider, TProvider>();
        return this;
    }

    /// <inheritdoc />
    public IAIBuilder RegisterVectorStore<TStore>()
        where TStore : class, IVectorStore
    {
        _services.AddSingleton<IVectorStore, TStore>();
        return this;
    }

    /// <inheritdoc />
    public IAIBuilder RegisterTextChunker<TChunker>()
        where TChunker : class, ITextChunker
    {
        _services.AddSingleton<ITextChunker, TChunker>();
        return this;
    }
}

/// <summary>
/// Storage for registered AI strategies.
/// Similar to StrategyStorage in Lucene integration.
/// </summary>
internal static class AIStrategyStorage
{
    public static Dictionary<string, Type> Strategies { get; } = [];

    public static void AddStrategy<TStrategy>(string strategyName)
        where TStrategy : IAIIndexingStrategy
    {
        Strategies[strategyName] = typeof(TStrategy);
    }

    public static Type GetOrDefault(string strategyName)
    {
        return Strategies.TryGetValue(strategyName, out var type)
            ? type
            : typeof(DefaultAIIndexingStrategy);
    }
}
