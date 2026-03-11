using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baseline.Search;

/// <summary>
/// JSON-file-backed implementation of <see cref="IIndexGenerationService"/>.
/// Records index rebuild snapshots and enforces a configurable retention policy.
/// </summary>
public sealed class IndexGenerationStorageService(
    IOptions<BaselineSearchOptions> options,
    ILogger<IndexGenerationStorageService> logger) : IIndexGenerationService
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly object _lock = new();
    private readonly string _storagePath = System.IO.Path.Combine(
        options.Value.Lucene.IndexPath, "index-generations.json");

    /// <inheritdoc />
    public Task<IndexGeneration> RecordGenerationAsync(
        string indexName, IndexStatistics stats, TimeSpan duration)
    {
        var generation = new IndexGeneration
        {
            Id = Guid.NewGuid().ToString("N"),
            IndexName = indexName,
            CreatedAt = DateTimeOffset.UtcNow,
            DocumentCount = stats.DocumentCount,
            IndexSizeBytes = stats.IndexSizeBytes,
            Duration = duration,
            DocumentCountByType = stats.DocumentCountByType,
            IsActive = true
        };

        lock (_lock)
        {
            var all = LoadAll();

            // Deactivate previous active generation for this index
            foreach (var g in all.Where(g => g.IndexName == indexName && g.IsActive))
            {
                g.IsActive = false;
            }

            all.Add(generation);
            SaveAll(all);
        }

        logger.LogInformation(
            "Recorded index generation {Id} for '{Index}': {Docs} docs, {Size} bytes, {Duration}ms",
            generation.Id, indexName, stats.DocumentCount, stats.IndexSizeBytes,
            (long)duration.TotalMilliseconds);

        return Task.FromResult(generation);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<IndexGeneration>> GetGenerationsAsync(string indexName)
    {
        lock (_lock)
        {
            var all = LoadAll();
            IReadOnlyList<IndexGeneration> result = all
                .Where(g => g.IndexName == indexName)
                .OrderByDescending(g => g.CreatedAt)
                .ToList();
            return Task.FromResult(result);
        }
    }

    /// <inheritdoc />
    public Task<IndexGeneration?> GetActiveGenerationAsync(string indexName)
    {
        lock (_lock)
        {
            var all = LoadAll();
            var active = all
                .Where(g => g.IndexName == indexName && g.IsActive)
                .OrderByDescending(g => g.CreatedAt)
                .FirstOrDefault();
            return Task.FromResult(active);
        }
    }

    /// <inheritdoc />
    public Task<int> ApplyRetentionPolicyAsync(string indexName, IndexRetentionPolicy? policy = null)
    {
        policy ??= options.Value.Lucene.Retention;
        int removed = 0;

        lock (_lock)
        {
            var all = LoadAll();
            var forIndex = all
                .Where(g => g.IndexName == indexName)
                .OrderByDescending(g => g.CreatedAt)
                .ToList();

            var cutoff = DateTimeOffset.UtcNow - policy.MaxAge;

            // Mark for removal: anything beyond max count OR older than max age (never remove active)
            var toRemove = new HashSet<string>();
            for (int i = 0; i < forIndex.Count; i++)
            {
                var g = forIndex[i];
                if (g.IsActive)
                {
                    continue;
                }

                bool tooMany = i >= policy.MaxGenerations;
                bool tooOld = g.CreatedAt < cutoff;

                if (tooMany || tooOld)
                {
                    toRemove.Add(g.Id);
                }
            }

            if (toRemove.Count > 0)
            {
                removed = all.RemoveAll(g => toRemove.Contains(g.Id));
                SaveAll(all);
                logger.LogInformation(
                    "Retention policy removed {Count} old generations for '{Index}'",
                    removed, indexName);
            }
        }

        return Task.FromResult(removed);
    }

    // --- persistence helpers ---

    private List<IndexGeneration> LoadAll()
    {
        try
        {
            if (!System.IO.File.Exists(_storagePath))
            {
                return [];
            }

            string json = System.IO.File.ReadAllText(_storagePath);
            return JsonSerializer.Deserialize<List<IndexGeneration>>(json, _jsonOptions) ?? [];
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to load index generations from {Path}", _storagePath);
            return [];
        }
    }

    private void SaveAll(List<IndexGeneration> generations)
    {
        try
        {
            var dir = System.IO.Path.GetDirectoryName(_storagePath);
            if (dir is not null)
            {
                System.IO.Directory.CreateDirectory(dir);
            }

            string json = JsonSerializer.Serialize(generations, _jsonOptions);
            System.IO.File.WriteAllText(_storagePath, json);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save index generations to {Path}", _storagePath);
        }
    }
}
