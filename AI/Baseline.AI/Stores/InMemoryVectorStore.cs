using System.Collections.Concurrent;

using Microsoft.Extensions.Logging;

namespace Baseline.AI.Stores;

/// <summary>
/// In-memory vector store for development and small datasets.
/// Uses brute-force cosine similarity search. Not suitable for large-scale production workloads.
/// </summary>
public sealed class InMemoryVectorStore(
    ILogger<InMemoryVectorStore> logger) : IVectorStore
{
    private readonly ConcurrentDictionary<string, StoredVector> _vectors = new();
    private readonly ILogger<InMemoryVectorStore> _logger = logger;

    /// <inheritdoc />
    public string StoreName => "InMemory";

    /// <inheritdoc />
    public Task UpsertAsync(AIDocument document, float[] embedding, CancellationToken cancellationToken = default)
    {
        _vectors[document.Id] = new StoredVector(document, embedding);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UpsertBatchAsync(
        IEnumerable<(AIDocument Document, float[] Embedding)> items,
        CancellationToken cancellationToken = default)
    {
        foreach (var (doc, emb) in items)
        {
            _vectors[doc.Id] = new StoredVector(doc, emb);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteAsync(string documentId, CancellationToken cancellationToken = default)
    {
        _vectors.TryRemove(documentId, out _);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteByContentItemAsync(Guid contentItemGuid, CancellationToken cancellationToken = default)
    {
        RemoveWhere(v => v.Document.ContentItemGuid == contentItemGuid);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteByKnowledgeBaseAsync(int knowledgeBaseId, CancellationToken cancellationToken = default)
    {
        string prefix = $"kb:{knowledgeBaseId}:";
        RemoveWhere(v => v.Document.Id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            || (v.Document.Metadata.TryGetValue("KnowledgeBaseId", out var kbId)
                && kbId is int id && id == knowledgeBaseId));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteByItemAsync(
        int knowledgeBaseId,
        Guid contentItemGuid,
        string? languageCode = null,
        int? channelId = null,
        CancellationToken cancellationToken = default)
    {
        RemoveWhere(v =>
        {
            if (v.Document.ContentItemGuid != contentItemGuid)
            {
                return false;
            }

            // Check KB via id prefix or metadata
            bool kbMatch = v.Document.Id.StartsWith($"kb:{knowledgeBaseId}:", StringComparison.OrdinalIgnoreCase)
                || (v.Document.Metadata.TryGetValue("KnowledgeBaseId", out var kbId) && kbId is int id && id == knowledgeBaseId);

            if (!kbMatch) return false;
            if (languageCode is not null && !string.Equals(v.Document.LanguageCode, languageCode, StringComparison.OrdinalIgnoreCase)) return false;
            if (channelId.HasValue && v.Document.Metadata.TryGetValue("ChannelId", out var ch) && ch is int cid && cid != channelId.Value) return false;

            return true;
        });

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        float[] queryEmbedding,
        int topK = 10,
        VectorSearchFilter? filter = null,
        CancellationToken cancellationToken = default)
    {
        var candidates = _vectors.Values.AsEnumerable();

        // Apply filters
        if (filter is not null)
        {
            candidates = ApplyFilter(candidates, filter);
        }

        // Compute cosine similarity and rank
        var results = candidates
            .Select(v => new
            {
                Vector = v,
                Score = CosineSimilarity(queryEmbedding, v.Embedding)
            })
            .Where(x => filter?.MinScore is null || x.Score >= filter.MinScore)
            .OrderByDescending(x => x.Score)
            .Take(topK)
            .Select(x => new VectorSearchResult
            {
                Document = x.Vector.Document,
                Score = x.Score,
                Distance = 1.0 - x.Score
            })
            .ToList();

        _logger.LogDebug("InMemory search: {TotalVectors} vectors, {FilteredResults} results returned", _vectors.Count, results.Count);

        return Task.FromResult<IReadOnlyList<VectorSearchResult>>(results);
    }

    /// <inheritdoc />
    public Task<AIDocument?> GetAsync(string documentId, CancellationToken cancellationToken = default)
    {
        _vectors.TryGetValue(documentId, out var stored);
        return Task.FromResult(stored?.Document);
    }

    /// <inheritdoc />
    public Task<int> CountAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_vectors.Count);

    /// <inheritdoc />
    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        _vectors.Clear();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("InMemory vector store initialized ({Count} vectors)", _vectors.Count);
        return Task.CompletedTask;
    }

    private static IEnumerable<StoredVector> ApplyFilter(IEnumerable<StoredVector> candidates, VectorSearchFilter filter)
    {
        if (filter.KnowledgeBaseId.HasValue)
        {
            int kbId = filter.KnowledgeBaseId.Value;
            string prefix = $"kb:{kbId}:";
            candidates = candidates.Where(v =>
                v.Document.Id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                || (v.Document.Metadata.TryGetValue("KnowledgeBaseId", out var id) && id is int mid && mid == kbId));
        }

        if (filter.ContentTypes is { Count: > 0 })
        {
            var types = new HashSet<string>(filter.ContentTypes, StringComparer.OrdinalIgnoreCase);
            candidates = candidates.Where(v => types.Contains(v.Document.ContentTypeName));
        }

        if (!string.IsNullOrWhiteSpace(filter.LanguageCode))
        {
            candidates = candidates.Where(v =>
                string.Equals(v.Document.LanguageCode, filter.LanguageCode, StringComparison.OrdinalIgnoreCase));
        }

        if (filter.ChannelId.HasValue)
        {
            int channelId = filter.ChannelId.Value;
            candidates = candidates.Where(v =>
                v.Document.Metadata.TryGetValue("ChannelId", out var ch) && ch is int cid && cid == channelId);
        }

        return candidates;
    }

    private static double CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length || a.Length == 0)
        {
            return 0;
        }

        double dotProduct = 0, normA = 0, normB = 0;

        for (int i = 0; i < a.Length; i++)
        {
            dotProduct += a[i] * (double)b[i];
            normA += a[i] * (double)a[i];
            normB += b[i] * (double)b[i];
        }

        double denominator = Math.Sqrt(normA) * Math.Sqrt(normB);
        return denominator == 0 ? 0 : dotProduct / denominator;
    }

    private void RemoveWhere(Func<StoredVector, bool> predicate)
    {
        var toRemove = _vectors.Where(kv => predicate(kv.Value)).Select(kv => kv.Key).ToList();
        foreach (string key in toRemove)
        {
            _vectors.TryRemove(key, out _);
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<AIDocument>> ListDocumentsAsync(
        VectorSearchFilter? filter = null,
        CancellationToken cancellationToken = default)
    {
        var candidates = _vectors.Values.AsEnumerable();

        if (filter is not null)
        {
            candidates = ApplyFilter(candidates, filter);
        }

        IReadOnlyList<AIDocument> result = candidates.Select(v => v.Document).ToList();
        return Task.FromResult(result);
    }

    private sealed record StoredVector(AIDocument Document, float[] Embedding);
}
