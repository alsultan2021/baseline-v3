namespace Baseline.AI;

/// <summary>
/// Interface for vector store operations - stores and retrieves embeddings.
/// Supports different backends (in-memory, file-based, database, external services).
/// </summary>
public interface IVectorStore
{
    /// <summary>
    /// Store name for identification.
    /// </summary>
    string StoreName { get; }

    /// <summary>
    /// Upserts a document with its embedding.
    /// </summary>
    /// <param name="document">Document to store.</param>
    /// <param name="embedding">Vector embedding.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpsertAsync(
        AIDocument document,
        float[] embedding,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Upserts multiple documents with embeddings.
    /// </summary>
    Task UpsertBatchAsync(
        IEnumerable<(AIDocument Document, float[] Embedding)> items,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a document by ID.
    /// </summary>
    Task DeleteAsync(string documentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes documents by content item GUID.
    /// </summary>
    Task DeleteByContentItemAsync(Guid contentItemGuid, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all vectors for a knowledge base.
    /// Used during full rebuild to clear before re-indexing.
    /// </summary>
    /// <param name="knowledgeBaseId">The knowledge base ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteByKnowledgeBaseAsync(int knowledgeBaseId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all vectors for a specific content item within a knowledge base.
    /// Vector key pattern: "kb:{kbId}:{contentGuid}:{lang}:{channelId}:{chunkIdx}"
    /// </summary>
    /// <param name="knowledgeBaseId">The knowledge base ID.</param>
    /// <param name="contentItemGuid">The content item GUID.</param>
    /// <param name="languageCode">Language code, or null for all languages.</param>
    /// <param name="channelId">Channel ID, or null for all channels.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteByItemAsync(
        int knowledgeBaseId,
        Guid contentItemGuid,
        string? languageCode = null,
        int? channelId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for similar documents.
    /// </summary>
    /// <param name="queryEmbedding">Query vector.</param>
    /// <param name="topK">Number of results to return.</param>
    /// <param name="filter">Optional metadata filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        float[] queryEmbedding,
        int topK = 10,
        VectorSearchFilter? filter = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a document by ID.
    /// </summary>
    Task<AIDocument?> GetAsync(string documentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total count of documents.
    /// </summary>
    Task<int> CountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all documents from the store.
    /// </summary>
    Task ClearAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Initializes the vector store (creates indices, etc.).
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all stored documents, optionally filtered.
    /// Used by hybrid search for keyword scanning.
    /// </summary>
    /// <param name="filter">Optional filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<AIDocument>> ListDocumentsAsync(
        VectorSearchFilter? filter = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Vector search result.
/// </summary>
public sealed class VectorSearchResult
{
    /// <summary>
    /// The matched document.
    /// </summary>
    public required AIDocument Document { get; init; }

    /// <summary>
    /// Similarity score (0.0 to 1.0).
    /// </summary>
    public double Score { get; init; }

    /// <summary>
    /// Distance metric value.
    /// </summary>
    public double? Distance { get; init; }
}

/// <summary>
/// Filter for vector search.
/// </summary>
public sealed class VectorSearchFilter
{
    /// <summary>
    /// Filter by knowledge base ID.
    /// </summary>
    public int? KnowledgeBaseId { get; init; }

    /// <summary>
    /// Filter by content type names.
    /// </summary>
    public IReadOnlyList<string>? ContentTypes { get; init; }

    /// <summary>
    /// Filter by language code.
    /// </summary>
    public string? LanguageCode { get; init; }

    /// <summary>
    /// Filter by channel ID (0 = reusable, >0 = specific channel).
    /// </summary>
    public int? ChannelId { get; init; }

    /// <summary>
    /// Filter by channel name.
    /// </summary>
    [Obsolete("Use ChannelId instead")]
    public string? ChannelName { get; init; }

    /// <summary>
    /// Custom metadata filters.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; init; }

    /// <summary>
    /// Minimum similarity score.
    /// </summary>
    public double? MinScore { get; init; }
}
