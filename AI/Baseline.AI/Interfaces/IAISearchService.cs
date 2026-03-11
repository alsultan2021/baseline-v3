namespace Baseline.AI;

/// <summary>
/// Main AI search service - combines embedding generation and vector search.
/// Similar to ILuceneSearchService in the Lucene integration.
/// </summary>
public interface IAISearchService
{
    /// <summary>
    /// Performs semantic search using AI embeddings.
    /// </summary>
    /// <param name="query">Search query.</param>
    /// <param name="options">Search options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Search results with similarity scores.</returns>
    Task<AISearchResult> SearchAsync(
        string query,
        AISearchOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an AI-generated answer for a question using RAG.
    /// </summary>
    /// <param name="question">The question to answer.</param>
    /// <param name="options">Answer options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>AI-generated answer with sources.</returns>
    Task<AIAnswer> GetAnswerAsync(
        string question,
        AIAnswerOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an AI-generated answer with streaming response.
    /// </summary>
    IAsyncEnumerable<AIAnswerChunk> GetAnswerStreamingAsync(
        string question,
        AIAnswerOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs hybrid search (combining keyword and semantic search).
    /// </summary>
    Task<AISearchResult> HybridSearchAsync(
        string query,
        AISearchOptions? options = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// AI search options.
/// </summary>
public sealed class AISearchOptions
{
    /// <summary>
    /// Number of results to return.
    /// </summary>
    public int TopK { get; init; } = 10;

    /// <summary>
    /// Minimum similarity score threshold.
    /// </summary>
    public double MinScore { get; init; } = 0.7;

    /// <summary>
    /// Filter by content types.
    /// </summary>
    public IReadOnlyList<string>? ContentTypes { get; init; }

    /// <summary>
    /// Filter by language.
    /// </summary>
    public string? LanguageCode { get; init; }

    /// <summary>
    /// Filter by channel.
    /// </summary>
    public string? ChannelName { get; init; }

    /// <summary>
    /// Include content snippets in results.
    /// </summary>
    public bool IncludeSnippets { get; init; } = true;

    /// <summary>
    /// Maximum snippet length.
    /// </summary>
    public int MaxSnippetLength { get; init; } = 200;

    /// <summary>
    /// Scope search to a specific knowledge base.
    /// </summary>
    public int? KnowledgeBaseId { get; init; }
}

/// <summary>
/// AI search result.
/// </summary>
public sealed class AISearchResult
{
    /// <summary>
    /// Search results.
    /// </summary>
    public required IReadOnlyList<AISearchHit> Hits { get; init; }

    /// <summary>
    /// Total count of matches.
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// Search query used.
    /// </summary>
    public required string Query { get; init; }

    /// <summary>
    /// Search duration in milliseconds.
    /// </summary>
    public long DurationMs { get; init; }
}

/// <summary>
/// Individual search hit.
/// </summary>
public sealed class AISearchHit
{
    /// <summary>
    /// Document ID.
    /// </summary>
    public required string DocumentId { get; init; }

    /// <summary>
    /// Content item ID.
    /// </summary>
    public int ContentItemId { get; init; }

    /// <summary>
    /// Content item GUID.
    /// </summary>
    public Guid ContentItemGuid { get; init; }

    /// <summary>
    /// Title.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// URL.
    /// </summary>
    public string? Url { get; init; }

    /// <summary>
    /// Content snippet.
    /// </summary>
    public string? Snippet { get; init; }

    /// <summary>
    /// Content type name.
    /// </summary>
    public required string ContentTypeName { get; init; }

    /// <summary>
    /// Similarity score.
    /// </summary>
    public double Score { get; init; }

    /// <summary>
    /// Additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = [];
}

/// <summary>
/// Options for AI answer generation.
/// </summary>
public sealed class AIAnswerOptions
{
    /// <summary>
    /// Number of context documents to use.
    /// </summary>
    public int TopK { get; init; } = 5;

    /// <summary>
    /// Include source citations.
    /// </summary>
    public bool IncludeSources { get; init; } = true;

    /// <summary>
    /// Custom system prompt.
    /// </summary>
    public string? SystemPrompt { get; init; }

    /// <summary>
    /// Maximum tokens for the response.
    /// </summary>
    public int? MaxTokens { get; init; }

    /// <summary>
    /// Temperature for response generation.
    /// </summary>
    public double Temperature { get; init; } = 0.7;

    /// <summary>
    /// Conversation history for context.
    /// </summary>
    public IReadOnlyList<AIChatMessage>? ConversationHistory { get; init; }

    /// <summary>
    /// Filter by content types.
    /// </summary>
    public IReadOnlyList<string>? ContentTypes { get; init; }

    /// <summary>
    /// Filter by language.
    /// </summary>
    public string? LanguageCode { get; init; }

    /// <summary>
    /// Scope search to a specific knowledge base.
    /// </summary>
    public int? KnowledgeBaseId { get; init; }
}

/// <summary>
/// AI-generated answer with sources.
/// </summary>
public sealed class AIAnswer
{
    /// <summary>
    /// The generated answer.
    /// </summary>
    public required string Answer { get; init; }

    /// <summary>
    /// Source documents used for the answer.
    /// </summary>
    public required IReadOnlyList<AISource> Sources { get; init; }

    /// <summary>
    /// The original question.
    /// </summary>
    public required string Question { get; init; }

    /// <summary>
    /// Confidence score (0.0 to 1.0).
    /// </summary>
    public double Confidence { get; init; }

    /// <summary>
    /// Token usage.
    /// </summary>
    public AITokenUsage? TokenUsage { get; init; }

    /// <summary>
    /// Processing duration in milliseconds.
    /// </summary>
    public long DurationMs { get; init; }
}

/// <summary>
/// Streaming answer chunk.
/// </summary>
public sealed class AIAnswerChunk
{
    /// <summary>
    /// Delta content.
    /// </summary>
    public string? Content { get; init; }

    /// <summary>
    /// Whether this is the final chunk.
    /// </summary>
    public bool IsComplete { get; init; }

    /// <summary>
    /// Sources (only on final chunk).
    /// </summary>
    public IReadOnlyList<AISource>? Sources { get; init; }
}

/// <summary>
/// Source citation for AI answer.
/// </summary>
public sealed class AISource
{
    /// <summary>
    /// Document ID.
    /// </summary>
    public required string DocumentId { get; init; }

    /// <summary>
    /// Title.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// URL.
    /// </summary>
    public string? Url { get; init; }

    /// <summary>
    /// Relevant snippet.
    /// </summary>
    public string? Snippet { get; init; }

    /// <summary>
    /// Relevance score.
    /// </summary>
    public double Score { get; init; }
}
