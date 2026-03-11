using Baseline.AI.Indexing;

namespace Baseline.AI;

/// <summary>
/// Interface for AI embedding strategies - similar to ILuceneIndexingStrategy.
/// Strategy EXTRACTS text + metadata; IAIChunkingService OWNS chunking.
/// </summary>
public interface IAIIndexingStrategy
{
    /// <summary>
    /// Strategy name for identification.
    /// Used in admin UI to select which strategy to use.
    /// </summary>
    string StrategyName { get; }

    /// <summary>
    /// Strategy display name for admin UI.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Extracts text + metadata from a content item (NO chunking here).
    /// </summary>
    /// <param name="item">The content item to index.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The extracted result, or null to skip.</returns>
    Task<AIExtractResult?> ExtractAsync(
        IAIIndexableItem item,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets chunking options for this strategy.
    /// </summary>
    ChunkingOptions GetChunkingOptions();

    /// <summary>
    /// Gets content type names that this strategy handles.
    /// </summary>
    IReadOnlyList<string> GetContentTypes();

    /// <summary>
    /// Determines if this strategy should process the given content type.
    /// </summary>
    bool ShouldProcess(string contentTypeName);

    /// <summary>
    /// Gets the fields to include in the embedding.
    /// </summary>
    IReadOnlyList<AIFieldDefinition> GetFieldDefinitions();

    /// <summary>
    /// Computes a deterministic hash of the strategy configuration.
    /// Used to detect strategy drift and trigger auto-rebuild.
    /// Return null to use default registry-calculated hash.
    /// </summary>
    string? ComputeStrategyHash();

    #region Legacy methods (deprecated - use ExtractAsync)

    /// <summary>
    /// Maps a content item to an AI document for embedding.
    /// DEPRECATED: Use ExtractAsync instead.
    /// </summary>
    [Obsolete("Use ExtractAsync instead")]
    Task<AIDocument?> MapToDocumentAsync(
        IAIIndexableItem item,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Preprocesses text before embedding (e.g., cleaning, summarizing).
    /// DEPRECATED: Preprocessing should happen in ExtractAsync.
    /// </summary>
    [Obsolete("Preprocessing should happen in ExtractAsync")]
    Task<string> PreprocessTextAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets metadata to store alongside the embedding.
    /// DEPRECATED: Return metadata in AIExtractResult instead.
    /// </summary>
    [Obsolete("Return metadata in AIExtractResult instead")]
    Task<Dictionary<string, object>> GetMetadataAsync(
        IAIIndexableItem item,
        CancellationToken cancellationToken = default);

    #endregion
}

/// <summary>
/// Interface for items that can be indexed with AI embeddings.
/// </summary>
public interface IAIIndexableItem
{
    /// <summary>
    /// Content item ID.
    /// </summary>
    int ContentItemId { get; }

    /// <summary>
    /// Content item GUID.
    /// </summary>
    Guid ContentItemGuid { get; }

    /// <summary>
    /// Content type name.
    /// </summary>
    string ContentTypeName { get; }

    /// <summary>
    /// Language code.
    /// </summary>
    string LanguageCode { get; }

    /// <summary>
    /// Channel ID (0 for reusable content, >0 for page content).
    /// This is the canonical channel identifier used in vector keys.
    /// </summary>
    int ChannelId { get; }

    /// <summary>
    /// Channel name (for web page items).
    /// </summary>
    string? ChannelName { get; }

    /// <summary>
    /// Whether this is a web page item (ChannelId > 0).
    /// </summary>
    bool IsWebPageItem { get; }

    /// <summary>
    /// URL path for web page items.
    /// </summary>
    string? UrlPath { get; }

    /// <summary>
    /// Gets a field value by name.
    /// </summary>
    object? GetFieldValue(string fieldName);

    /// <summary>
    /// Gets a strongly-typed field value.
    /// </summary>
    T? GetFieldValue<T>(string fieldName);

    /// <summary>
    /// Gets all field names.
    /// </summary>
    IReadOnlyList<string> GetFieldNames();
}

/// <summary>
/// AI document to be embedded and stored.
/// </summary>
public sealed class AIDocument
{
    /// <summary>
    /// Unique identifier for the document.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Content item ID.
    /// </summary>
    public int ContentItemId { get; init; }

    /// <summary>
    /// Content item GUID.
    /// </summary>
    public Guid ContentItemGuid { get; init; }

    /// <summary>
    /// The text content to embed.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Title for display purposes.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// URL for the content.
    /// </summary>
    public string? Url { get; init; }

    /// <summary>
    /// Content type name.
    /// </summary>
    public required string ContentTypeName { get; init; }

    /// <summary>
    /// Language code.
    /// </summary>
    public required string LanguageCode { get; init; }

    /// <summary>
    /// Additional metadata stored with the embedding.
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = [];

    /// <summary>
    /// When the document was last updated.
    /// </summary>
    public DateTime LastModified { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Document chunks for large content.
    /// </summary>
    public List<AIDocumentChunk> Chunks { get; init; } = [];
}

/// <summary>
/// A chunk of a larger document for embedding.
/// </summary>
public sealed class AIDocumentChunk
{
    /// <summary>
    /// Chunk identifier.
    /// </summary>
    public required string ChunkId { get; init; }

    /// <summary>
    /// Chunk index (0-based).
    /// </summary>
    public int ChunkIndex { get; init; }

    /// <summary>
    /// Text content of the chunk.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Start character position in original content.
    /// </summary>
    public int StartPosition { get; init; }

    /// <summary>
    /// End character position in original content.
    /// </summary>
    public int EndPosition { get; init; }
}

/// <summary>
/// Field definition for AI indexing.
/// </summary>
public sealed class AIFieldDefinition
{
    /// <summary>
    /// Field name in the content type.
    /// </summary>
    public required string FieldName { get; init; }

    /// <summary>
    /// Whether to include in the embedding text.
    /// </summary>
    public bool IncludeInEmbedding { get; init; } = true;

    /// <summary>
    /// Whether to store as metadata.
    /// </summary>
    public bool StoreAsMetadata { get; init; } = false;

    /// <summary>
    /// Weight for this field in the embedding (1.0 = normal).
    /// </summary>
    public double Weight { get; init; } = 1.0;

    /// <summary>
    /// Custom preprocessor for this field.
    /// </summary>
    public Func<object?, string>? Preprocessor { get; init; }
}
