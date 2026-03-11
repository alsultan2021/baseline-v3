namespace Baseline.AI;

/// <summary>
/// Interface for AI embedding service - manages content embedding and indexing.
/// Similar to how Lucene has ILuceneTaskProcessor for indexing.
/// </summary>
public interface IAIEmbeddingService
{
    /// <summary>
    /// Generates an embedding for the given text.
    /// </summary>
    Task<float[]> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates embeddings for multiple texts.
    /// </summary>
    Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(
        IEnumerable<string> texts,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Indexes a content item (generates embedding and stores).
    /// </summary>
    Task IndexContentAsync(
        IAIIndexableItem item,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Indexes multiple content items.
    /// </summary>
    Task IndexContentBatchAsync(
        IEnumerable<IAIIndexableItem> items,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a content item from the index.
    /// </summary>
    Task RemoveContentAsync(
        Guid contentItemGuid,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Rebuilds the entire AI index.
    /// </summary>
    Task RebuildIndexAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the embedding dimension size.
    /// </summary>
    int EmbeddingDimensions { get; }
}

/// <summary>
/// Interface for text chunking - splits content into manageable pieces.
/// </summary>
public interface ITextChunker
{
    /// <summary>
    /// Splits text into chunks suitable for embedding.
    /// </summary>
    /// <param name="text">Text to split.</param>
    /// <param name="maxChunkTokens">Maximum tokens per chunk.</param>
    /// <param name="overlapTokens">Token overlap between chunks.</param>
    /// <returns>List of text chunks.</returns>
    IReadOnlyList<TextChunk> ChunkText(
        string text,
        int maxChunkTokens = 512,
        int overlapTokens = 50);
}

/// <summary>
/// Text chunk with position information.
/// </summary>
public sealed class TextChunk
{
    /// <summary>
    /// Chunk index.
    /// </summary>
    public int Index { get; init; }

    /// <summary>
    /// Chunk content.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Start character position.
    /// </summary>
    public int StartPosition { get; init; }

    /// <summary>
    /// End character position.
    /// </summary>
    public int EndPosition { get; init; }

    /// <summary>
    /// Estimated token count.
    /// </summary>
    public int EstimatedTokens { get; init; }
}
