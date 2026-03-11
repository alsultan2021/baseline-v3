using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baseline.Search;

/// <summary>
/// Semantic search using configurable embedding providers and disk-persisted vector store.
/// Supports hybrid search (semantic + keyword) with configurable weighting.
/// </summary>
public class SemanticSearchService : ISemanticSearchService, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IEmbeddingProvider _embeddingProvider;
    private readonly SemanticSearchOptions _options;
    private readonly ILogger<SemanticSearchService> _logger;

    // Disk-backed vector store
    private readonly string _vectorStorePath;
    private readonly object _lock = new();
    private Dictionary<string, float[]>? _vectorStore;
    private bool _dirty;
    private readonly Timer _flushTimer;

    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public SemanticSearchService(
        IServiceScopeFactory scopeFactory,
        IEmbeddingProvider embeddingProvider,
        IOptions<BaselineSearchOptions> options,
        ILogger<SemanticSearchService> logger)
    {
        _scopeFactory = scopeFactory;
        _embeddingProvider = embeddingProvider;
        _options = options.Value.Semantic;
        _logger = logger;

        _vectorStorePath = System.IO.Path.Combine(_options.VectorStoragePath, "vectors.json");

        // Flush dirty vectors every 5 minutes
        _flushTimer = new Timer(_ => FlushToDisk(), null,
            TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    /// <inheritdoc />
    public async Task<SearchResults> SemanticSearchAsync(string query, int topK = 10)
    {
        try
        {
            var queryEmbedding = await GenerateEmbeddingAsync(query);
            var similarDocs = FindSimilarDocuments(queryEmbedding, topK);

            var results = new SearchResults
            {
                Query = query,
                Items = similarDocs.Select(d => new SearchResult
                {
                    Id = d.DocumentId,
                    Score = d.Similarity,
                    Title = d.DocumentId
                }).ToList(),
                TotalCount = similarDocs.Count
            };

            // Hybrid: merge with keyword results
            if (_options.EnableHybridSearch)
            {
                using var scope = _scopeFactory.CreateScope();
                var searchService = scope.ServiceProvider.GetRequiredService<ISearchService>();
                var keywordResults = await searchService.SearchAsync(new SearchRequest
                {
                    Query = query,
                    PageSize = topK
                });

                results = MergeResults(results, keywordResults, _options.SemanticWeight);
            }

            _logger.LogDebug("Semantic search '{Query}': {Count} results", query, results.TotalCount);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Semantic search failed for '{Query}'", query);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<SearchResult>> GetSimilarContentAsync(string documentId, int limit = 5)
    {
        var store = GetVectorStore();

        float[]? embedding;
        lock (_lock)
        {
            if (!store.TryGetValue(documentId, out embedding))
            {
                _logger.LogWarning("No embedding for document '{Id}'", documentId);
                return [];
            }
        }

        var similar = FindSimilarDocuments(embedding, limit + 1)
            .Where(d => d.DocumentId != documentId)
            .Take(limit);

        return similar.Select(d => new SearchResult
        {
            Id = d.DocumentId,
            Score = d.Similarity
        });
    }

    /// <inheritdoc />
    public Task<float[]> GenerateEmbeddingAsync(string text)
        => _embeddingProvider.GenerateEmbeddingAsync(text);

    /// <inheritdoc />
    public Task IndexEmbeddingAsync(string documentId, float[] embedding)
    {
        if (embedding.Length != _options.EmbeddingDimensions)
        {
            throw new ArgumentException(
                $"Embedding dimension mismatch: expected {_options.EmbeddingDimensions}, got {embedding.Length}");
        }

        var store = GetVectorStore();
        lock (_lock)
        {
            store[documentId] = embedding;
            _dirty = true;
        }

        _logger.LogDebug("Indexed embedding for '{Id}'", documentId);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _flushTimer.Dispose();
        FlushToDisk();
        GC.SuppressFinalize(this);
    }

    // --- vector search ---

    private List<(string DocumentId, double Similarity)> FindSimilarDocuments(
        float[] queryEmbedding, int topK)
    {
        var store = GetVectorStore();
        var results = new List<(string DocumentId, double Similarity)>();

        lock (_lock)
        {
            foreach (var (id, vec) in store)
            {
                double sim = CosineSimilarity(queryEmbedding, vec);
                if (sim >= _options.MinSimilarityThreshold)
                {
                    results.Add((id, sim));
                }
            }
        }

        return results
            .OrderByDescending(r => r.Similarity)
            .Take(topK)
            .ToList();
    }

    private static double CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length) return 0;

        double dot = 0, normA = 0, normB = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        return (normA == 0 || normB == 0) ? 0 : dot / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }

    private static SearchResults MergeResults(
        SearchResults semantic, SearchResults keyword, double semanticWeight)
    {
        var merged = new Dictionary<string, SearchResult>();

        foreach (var r in semantic.Items)
        {
            r.Score *= semanticWeight;
            merged[r.Id] = r;
        }

        foreach (var r in keyword.Items)
        {
            if (merged.TryGetValue(r.Id, out var existing))
            {
                existing.Score += r.Score * (1 - semanticWeight);
                existing.Title = r.Title;
                existing.Description = r.Description;
                existing.Url = r.Url;
            }
            else
            {
                r.Score *= (1 - semanticWeight);
                merged[r.Id] = r;
            }
        }

        return new SearchResults
        {
            Query = semantic.Query,
            Items = merged.Values.OrderByDescending(r => r.Score).ToList(),
            TotalCount = merged.Count
        };
    }

    // --- disk persistence ---

    private Dictionary<string, float[]> GetVectorStore()
    {
        if (_vectorStore is not null) return _vectorStore;

        lock (_lock)
        {
            _vectorStore ??= LoadFromDisk();
            return _vectorStore;
        }
    }

    private Dictionary<string, float[]> LoadFromDisk()
    {
        try
        {
            if (!System.IO.File.Exists(_vectorStorePath))
                return [];

            string json = System.IO.File.ReadAllText(_vectorStorePath);
            return JsonSerializer.Deserialize<Dictionary<string, float[]>>(json, _jsonOpts) ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load vector store from {Path}", _vectorStorePath);
            return [];
        }
    }

    private void FlushToDisk()
    {
        lock (_lock)
        {
            if (!_dirty || _vectorStore is null) return;

            try
            {
                var dir = System.IO.Path.GetDirectoryName(_vectorStorePath);
                if (dir is not null)
                    System.IO.Directory.CreateDirectory(dir);

                string json = JsonSerializer.Serialize(_vectorStore, _jsonOpts);
                System.IO.File.WriteAllText(_vectorStorePath, json);
                _dirty = false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to flush vector store to {Path}", _vectorStorePath);
            }
        }
    }
}
