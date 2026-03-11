using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baseline.AI.Services;

/// <summary>
/// Default implementation of taxonomy embedding service.
/// Manages embeddings for taxonomy tags to enable semantic matching.
/// </summary>
public class DefaultTaxonomyEmbeddingService(
    IAIEmbeddingService embeddingService,
    IVectorStore vectorStore,
    ITaxonomyProvider taxonomyProvider,
    IMemoryCache cache,
    IOptions<BaselineAIOptions> options,
    ILogger<DefaultTaxonomyEmbeddingService> logger) : ITaxonomyEmbeddingService
{
    private readonly IAIEmbeddingService _embeddingService = embeddingService;
    private readonly IVectorStore _vectorStore = vectorStore;
    private readonly ITaxonomyProvider _taxonomyProvider = taxonomyProvider;
    private readonly IMemoryCache _cache = cache;
    private readonly BaselineAIOptions _options = options.Value;
    private readonly ILogger<DefaultTaxonomyEmbeddingService> _logger = logger;

    private const string TAXONOMY_INDEX_PREFIX = "taxonomy_";
    private const string TAG_CACHE_PREFIX = "tag_embedding_";

    /// <inheritdoc />
    public async Task BuildTaxonomyEmbeddingsAsync(
        string taxonomyName,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Building embeddings for taxonomy: {TaxonomyName}", taxonomyName);

        var tags = await _taxonomyProvider.GetTagsAsync(taxonomyName, cancellationToken);

        foreach (var tag in tags)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                // Build text for embedding (name + optional description)
                var textToEmbed = _options.AutoTagging.IncludeTagDescriptions && !string.IsNullOrEmpty(tag.Description)
                    ? $"{tag.Name}: {tag.Description}"
                    : tag.Name;

                var embedding = await _embeddingService.GenerateEmbeddingAsync(textToEmbed, cancellationToken);

                // Create AIDocument for the tag
                var document = new AIDocument
                {
                    Id = $"{GetTaxonomyIndexName(taxonomyName)}_{tag.TagGuid}",
                    Content = textToEmbed,
                    ContentTypeName = "Taxonomy.Tag",
                    LanguageCode = "en",
                    Metadata = new Dictionary<string, object>
                    {
                        ["tagGuid"] = tag.TagGuid.ToString(),
                        ["name"] = tag.Name,
                        ["taxonomyName"] = taxonomyName,
                        ["description"] = tag.Description ?? string.Empty
                    }
                };

                // Store in vector store
                await _vectorStore.UpsertAsync(document, embedding, cancellationToken);

                // Cache the embedding
                var cacheKey = $"{TAG_CACHE_PREFIX}{tag.TagGuid}";
                _cache.Set(cacheKey, embedding, TimeSpan.FromHours(24));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to build embedding for tag: {TagName}", tag.Name);
            }
        }

        _logger.LogInformation("Completed building embeddings for taxonomy: {TaxonomyName} ({Count} tags)",
            taxonomyName, tags.Count);
    }

    /// <inheritdoc />
    public async Task RebuildAllTaxonomyEmbeddingsAsync(CancellationToken cancellationToken = default)
    {
        var taxonomies = await _taxonomyProvider.GetTaxonomiesAsync(cancellationToken);

        foreach (var taxonomy in taxonomies)
        {
            await BuildTaxonomyEmbeddingsAsync(taxonomy.Name, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TagMatch>> FindSimilarTagsAsync(
        string text,
        string? taxonomyName = null,
        int topK = 10,
        CancellationToken cancellationToken = default)
    {
        // Generate embedding for the input text
        var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(text, cancellationToken);

        var results = new List<TagMatch>();

        if (!string.IsNullOrEmpty(taxonomyName))
        {
            // Search specific taxonomy
            var matches = await SearchTaxonomyAsync(taxonomyName, queryEmbedding, topK, cancellationToken);
            results.AddRange(matches);
        }
        else
        {
            // Search all taxonomies
            var taxonomies = await _taxonomyProvider.GetTaxonomiesAsync(cancellationToken);
            foreach (var taxonomy in taxonomies)
            {
                var matches = await SearchTaxonomyAsync(taxonomy.Name, queryEmbedding, topK, cancellationToken);
                results.AddRange(matches);
            }

            // Sort by score and take top K overall
            results = results.OrderByDescending(r => r.Score).Take(topK).ToList();
        }

        return results;
    }

    /// <inheritdoc />
    public async Task<float[]?> GetTagEmbeddingAsync(
        Guid tagGuid,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{TAG_CACHE_PREFIX}{tagGuid}";

        if (_cache.TryGetValue(cacheKey, out float[]? cached))
        {
            return cached;
        }

        // Try to retrieve from vector store by searching for the specific tag
        var taxonomies = await _taxonomyProvider.GetTaxonomiesAsync(cancellationToken);

        foreach (var taxonomy in taxonomies)
        {
            var documentId = $"{GetTaxonomyIndexName(taxonomy.Name)}_{tagGuid}";
            var document = await _vectorStore.GetAsync(documentId, cancellationToken);

            if (document != null)
            {
                // We found the document, but IVectorStore.GetAsync returns AIDocument which doesn't have the embedding
                // The embedding would need to be retrieved by re-generating it from the content
                // For now, regenerate the embedding from the stored content
                var embedding = await _embeddingService.GenerateEmbeddingAsync(document.Content, cancellationToken);
                _cache.Set(cacheKey, embedding, TimeSpan.FromHours(24));
                return embedding;
            }
        }

        return null;
    }

    private async Task<IReadOnlyList<TagMatch>> SearchTaxonomyAsync(
        string taxonomyName,
        float[] queryEmbedding,
        int topK,
        CancellationToken cancellationToken)
    {
        try
        {
            var indexName = GetTaxonomyIndexName(taxonomyName);

            // Use VectorSearchFilter to filter by taxonomy and set minimum score
            var filter = new VectorSearchFilter
            {
                MinScore = _options.AutoTagging.MinConfidence,
                Metadata = new Dictionary<string, object>
                {
                    ["taxonomyName"] = taxonomyName
                }
            };

            var searchResults = await _vectorStore.SearchAsync(
                queryEmbedding,
                topK,
                filter,
                cancellationToken);

            var matches = new List<TagMatch>();

            foreach (var result in searchResults)
            {
                if (result.Document.Metadata.TryGetValue("tagGuid", out var tagGuidObj) &&
                    Guid.TryParse(tagGuidObj?.ToString(), out var tagGuid))
                {
                    matches.Add(new TagMatch
                    {
                        TagGuid = tagGuid,
                        Name = result.Document.Metadata.TryGetValue("name", out var nameObj) ? nameObj?.ToString() ?? string.Empty : string.Empty,
                        TaxonomyName = taxonomyName,
                        Score = (float)result.Score
                    });
                }
            }

            return matches;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to search taxonomy: {TaxonomyName}", taxonomyName);
            return [];
        }
    }

    private static string GetTaxonomyIndexName(string taxonomyName)
    {
        return $"{TAXONOMY_INDEX_PREFIX}{taxonomyName.ToLowerInvariant()}";
    }
}
