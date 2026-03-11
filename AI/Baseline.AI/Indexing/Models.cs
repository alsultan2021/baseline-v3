namespace Baseline.AI.Indexing;

/// <summary>
/// Result of strategy extraction - canonical text + metadata for indexing.
/// Strategy extracts; chunker chunks.
/// </summary>
public sealed record AIExtractResult(
    /// <summary>Preprocessed full text (sanitized, ready for chunking)</summary>
    string Content,
    /// <summary>Content title for citation</summary>
    string? Title,
    /// <summary>Full URL for citation</summary>
    string? Url,
    /// <summary>URL path for filtering (e.g., "/products/tiramisu")</summary>
    string? UrlPath,
    /// <summary>Content type name (e.g., "Chevalroyal.Product")</summary>
    string ContentTypeName,
    /// <summary>Language code (e.g., "en")</summary>
    string LanguageCode,
    /// <summary>Channel ID: 0 = reusable, > 0 = page</summary>
    int ChannelId,
    /// <summary>Channel name for display only</summary>
    string? ChannelName,
    /// <summary>Content item ID for admin/debug</summary>
    int ContentItemId,
    /// <summary>Content item GUID (stable identifier)</summary>
    Guid ContentItemGuid,
    /// <summary>Last modified timestamp</summary>
    DateTime LastModified,
    /// <summary>Custom metadata (tags, category, etc.)</summary>
    Dictionary<string, object> Metadata
);

/// <summary>
/// A text chunk created by IAIChunkingService.
/// </summary>
/// <param name="Content">Chunk text content</param>
/// <param name="ChunkIndex">Zero-based chunk index</param>
public sealed record TextChunk(string Content, int ChunkIndex);

/// <summary>
/// Chunking configuration options.
/// </summary>
public sealed class ChunkingOptions
{
    /// <summary>Maximum characters per chunk (default 2000)</summary>
    public int MaxChunkSize { get; set; } = 2000;

    /// <summary>Overlap characters between chunks (default 200)</summary>
    public int ChunkOverlap { get; set; } = 200;

    /// <summary>Split on paragraph boundaries when possible</summary>
    public bool SplitOnParagraphs { get; set; } = true;
}

/// <summary>
/// Index operation for queue processing.
/// </summary>
public sealed record IndexOperation(
    /// <summary>Operation type (Reconcile, Delete)</summary>
    IndexOperationType Type,
    /// <summary>Knowledge base ID</summary>
    int KnowledgeBaseId,
    /// <summary>Content item GUID</summary>
    Guid ContentItemGuid,
    /// <summary>Language code</summary>
    string LanguageCode,
    /// <summary>Channel ID: 0=reusable, >0=page, null=all channels</summary>
    int? ChannelId = null
);

/// <summary>
/// Index operation types.
/// </summary>
public enum IndexOperationType
{
    /// <summary>Re-check and upsert if matches config (default for publish/update)</summary>
    Reconcile = 0,
    /// <summary>Delete from index</summary>
    Delete = 1
}

/// <summary>
/// Vector record for storage - contains embedding + all metadata.
/// Key format: "kb:{kbId}:{contentGuid}:{lang}:{channelId}:{chunkIdx}"
/// </summary>
public sealed class AIVectorRecord
{
    /// <summary>Primary key: "kb:{kbId}:{contentGuid}:{lang}:{channelId}:{chunkIdx}"</summary>
    public required string Id { get; init; }

    /// <summary>Vector embedding</summary>
    public required float[] Vector { get; init; }

    #region Filtering Metadata (INDEXED)

    public int KnowledgeBaseId { get; init; }
    public int ContentItemId { get; init; }
    public Guid ContentItemGuid { get; init; }
    public required string LanguageCode { get; init; }
    /// <summary>0 = reusable (matches all channels), > 0 = page</summary>
    public int ChannelId { get; init; }
    public string? ChannelName { get; init; }
    public required string ContentTypeName { get; init; }
    public string? UrlPath { get; init; }
    public DateTime LastModified { get; init; }
    public string? ContentFingerprint { get; init; }

    #endregion

    #region Chunk Info

    public int ChunkIndex { get; init; }
    public int TotalChunks { get; init; }
    public required string ChunkContent { get; init; }

    #endregion

    #region Citation Metadata (STORED, not indexed)

    public string? Title { get; init; }
    public string? Url { get; init; }
    public string? HeadingPath { get; init; }

    #endregion

    /// <summary>
    /// Generates vector record ID following the standard format.
    /// </summary>
    public static string GenerateId(int kbId, Guid contentGuid, string lang, int channelId, int chunkIdx)
        => $"kb:{kbId}:{contentGuid}:{lang}:{channelId}:{chunkIdx}";
}

/// <summary>
/// Retrieval options for vector search.
/// </summary>
public sealed class RetrievalOptions
{
    public string? LanguageCode { get; set; }
    public int? ChannelId { get; set; }
    public bool AllowLanguageFallback { get; set; }
    public string? FallbackLanguageCode { get; set; }
    public double MinRelevanceThreshold { get; set; } = 0.5;
    public int TopK { get; set; } = 5;
}
