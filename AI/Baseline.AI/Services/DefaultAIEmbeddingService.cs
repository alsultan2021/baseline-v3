// Suppress obsolete warnings - this service uses legacy methods during transition
#pragma warning disable CS0618

using Baseline.AI.Indexing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baseline.AI;

/// <summary>
/// Default implementation of IAIEmbeddingService.
/// </summary>
internal sealed class DefaultAIEmbeddingService : IAIEmbeddingService
{
    private readonly IAIProvider? _aiProvider;
    private readonly IVectorStore? _vectorStore;
    private readonly IAIStrategyRegistry _strategyRegistry;
    private readonly ITextChunker _textChunker;
    private readonly BaselineAIOptions _options;
    private readonly ILogger<DefaultAIEmbeddingService> _logger;

    /// <inheritdoc />
    public int EmbeddingDimensions => _options.EmbeddingDimensions;

    public DefaultAIEmbeddingService(
        IOptions<BaselineAIOptions> options,
        IAIStrategyRegistry strategyRegistry,
        ITextChunker textChunker,
        ILogger<DefaultAIEmbeddingService> logger,
        IAIProvider? aiProvider = null,
        IVectorStore? vectorStore = null)
    {
        _options = options.Value;
        _strategyRegistry = strategyRegistry;
        _textChunker = textChunker;
        _logger = logger;
        _aiProvider = aiProvider;
        _vectorStore = vectorStore;
    }

    /// <inheritdoc />
    public async Task<float[]> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        if (_aiProvider == null)
        {
            throw new InvalidOperationException("AI provider not configured");
        }

        return await _aiProvider.GenerateEmbeddingAsync(text, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(
        IEnumerable<string> texts,
        CancellationToken cancellationToken = default)
    {
        if (_aiProvider == null)
        {
            throw new InvalidOperationException("AI provider not configured");
        }

        return await _aiProvider.GenerateEmbeddingsAsync(texts, cancellationToken);
    }

    /// <inheritdoc />
    public async Task IndexContentAsync(
        IAIIndexableItem item,
        CancellationToken cancellationToken = default)
    {
        if (_aiProvider == null || _vectorStore == null)
        {
            _logger.LogWarning("AI provider or vector store not configured - skipping indexing");
            return;
        }

        // Find appropriate strategy
        var strategy = FindStrategy(item.ContentTypeName);
        if (strategy == null || !strategy.ShouldProcess(item.ContentTypeName))
        {
            _logger.LogDebug("No strategy for content type {ContentType}", item.ContentTypeName);
            return;
        }

        try
        {
            // Map to AI document
            var document = await strategy.MapToDocumentAsync(item, cancellationToken);
            if (document == null)
            {
                _logger.LogDebug("Strategy returned null for item {ItemId}", item.ContentItemId);
                return;
            }

            // Preprocess content
            var processedContent = await strategy.PreprocessTextAsync(
                document.Content,
                cancellationToken);

            // Check if we need to chunk
            var chunks = _textChunker.ChunkText(
                processedContent,
                _options.MaxChunkTokens,
                _options.ChunkOverlapTokens);

            if (chunks.Count == 1)
            {
                // Single chunk - embed directly
                var embedding = await _aiProvider.GenerateEmbeddingAsync(
                    processedContent,
                    cancellationToken);

                await _vectorStore.UpsertAsync(document, embedding, cancellationToken);
            }
            else
            {
                // Multiple chunks - embed each
                var chunkItems = new List<(AIDocument, float[])>();

                foreach (var chunk in chunks)
                {
                    var chunkDoc = new AIDocument
                    {
                        Id = $"{document.Id}_chunk_{chunk.Index}",
                        ContentItemId = document.ContentItemId,
                        ContentItemGuid = document.ContentItemGuid,
                        Content = chunk.Content,
                        Title = document.Title,
                        Url = document.Url,
                        ContentTypeName = document.ContentTypeName,
                        LanguageCode = document.LanguageCode,
                        Metadata = new Dictionary<string, object>(document.Metadata)
                        {
                            ["chunkIndex"] = chunk.Index,
                            ["totalChunks"] = chunks.Count,
                            ["parentId"] = document.Id
                        },
                        LastModified = document.LastModified
                    };

                    var embedding = await _aiProvider.GenerateEmbeddingAsync(
                        chunk.Content,
                        cancellationToken);

                    chunkItems.Add((chunkDoc, embedding));
                }

                await _vectorStore.UpsertBatchAsync(chunkItems, cancellationToken);
            }

            _logger.LogInformation(
                "Indexed content item {ItemId} with {ChunkCount} chunk(s)",
                item.ContentItemId,
                chunks.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to index content item {ItemId}", item.ContentItemId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task IndexContentBatchAsync(
        IEnumerable<IAIIndexableItem> items,
        CancellationToken cancellationToken = default)
    {
        foreach (var item in items)
        {
            await IndexContentAsync(item, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task RemoveContentAsync(
        Guid contentItemGuid,
        CancellationToken cancellationToken = default)
    {
        if (_vectorStore == null)
        {
            return;
        }

        await _vectorStore.DeleteByContentItemAsync(contentItemGuid, cancellationToken);
        _logger.LogInformation("Removed content item {Guid} from AI index", contentItemGuid);
    }

    /// <inheritdoc />
    public async Task RebuildIndexAsync(CancellationToken cancellationToken = default)
    {
        if (_vectorStore == null)
        {
            _logger.LogWarning("Vector store not configured - cannot rebuild index");
            return;
        }

        await _vectorStore.ClearAsync(cancellationToken);
        await _vectorStore.InitializeAsync(cancellationToken);

        _logger.LogInformation("AI index cleared and reinitialized for rebuild");

        // Note: The actual content re-indexing would be triggered by the application
        // or a scheduled task that iterates through content items
    }

    private IAIIndexingStrategy? FindStrategy(string contentTypeName)
    {
        foreach (var strategyName in _strategyRegistry.GetStrategyNames())
        {
            var strategy = _strategyRegistry.GetStrategy(strategyName);
            if (strategy?.ShouldProcess(contentTypeName) == true)
            {
                return strategy;
            }
        }

        // Fall back to default strategy
        return _strategyRegistry.GetStrategy("Default");
    }
}
