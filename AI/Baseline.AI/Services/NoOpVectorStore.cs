namespace Baseline.AI.Services;

/// <summary>
/// No-op implementation of <see cref="IVectorStore"/> for scenarios where vector storage is not configured.
/// Allows AI services to run without a vector store dependency.
/// </summary>
internal sealed class NoOpVectorStore : IVectorStore
{
    public string StoreName => "NoOp";

    public Task UpsertAsync(AIDocument document, float[] embedding, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task UpsertBatchAsync(IEnumerable<(AIDocument Document, float[] Embedding)> items, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task DeleteAsync(string documentId, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task DeleteByContentItemAsync(Guid contentItemGuid, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task DeleteByKnowledgeBaseAsync(int knowledgeBaseId, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task DeleteByItemAsync(int knowledgeBaseId, Guid contentItemGuid, string? languageCode = null, int? channelId = null, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        float[] queryEmbedding,
        int topK = 10,
        VectorSearchFilter? filter = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<VectorSearchResult>>(Array.Empty<VectorSearchResult>());

    public Task<AIDocument?> GetAsync(string documentId, CancellationToken cancellationToken = default)
        => Task.FromResult<AIDocument?>(null);

    public Task<int> CountAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(0);

    public Task ClearAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task InitializeAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<IReadOnlyList<AIDocument>> ListDocumentsAsync(
        VectorSearchFilter? filter = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<AIDocument>>(Array.Empty<AIDocument>());
}
