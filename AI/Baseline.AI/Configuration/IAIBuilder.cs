namespace Baseline.AI;

/// <summary>
/// Builder for configuring Baseline AI services.
/// Similar to ILuceneBuilder pattern used in Kentico Lucene integration.
/// </summary>
public interface IAIBuilder
{
    /// <summary>
    /// If true, the DefaultAIIndexingStrategy will be available.
    /// Defaults to true.
    /// </summary>
    bool IncludeDefaultStrategy { get; set; }

    /// <summary>
    /// Registers a custom AI indexing strategy.
    /// </summary>
    /// <typeparam name="TStrategy">Strategy type implementing IAIIndexingStrategy.</typeparam>
    /// <param name="strategyName">Unique name for the strategy.</param>
    /// <returns>The builder for chaining.</returns>
    IAIBuilder RegisterStrategy<TStrategy>(string strategyName)
        where TStrategy : class, IAIIndexingStrategy;

    /// <summary>
    /// Registers a custom AI provider.
    /// </summary>
    /// <typeparam name="TProvider">Provider type implementing IAIProvider.</typeparam>
    /// <returns>The builder for chaining.</returns>
    IAIBuilder RegisterProvider<TProvider>()
        where TProvider : class, IAIProvider;

    /// <summary>
    /// Registers a custom vector store.
    /// </summary>
    /// <typeparam name="TStore">Store type implementing IVectorStore.</typeparam>
    /// <returns>The builder for chaining.</returns>
    IAIBuilder RegisterVectorStore<TStore>()
        where TStore : class, IVectorStore;

    /// <summary>
    /// Registers a custom text chunker.
    /// </summary>
    /// <typeparam name="TChunker">Chunker type implementing ITextChunker.</typeparam>
    /// <returns>The builder for chaining.</returns>
    IAIBuilder RegisterTextChunker<TChunker>()
        where TChunker : class, ITextChunker;
}
