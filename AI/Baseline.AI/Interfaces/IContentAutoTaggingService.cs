namespace Baseline.AI;

/// <summary>
/// Interface for AI-powered content tagging service.
/// Automatically suggests and applies taxonomy tags to content items.
/// </summary>
public interface IContentAutoTaggingService
{
    /// <summary>
    /// Suggests tags for content based on AI analysis.
    /// </summary>
    /// <param name="content">Content to analyze.</param>
    /// <param name="options">Tagging options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Suggested tags with confidence scores.</returns>
    Task<TagSuggestionResult> SuggestTagsAsync(
        ContentToTag content,
        TaggingOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Suggests tags for a content item by GUID.
    /// </summary>
    /// <param name="contentItemGuid">Content item GUID.</param>
    /// <param name="options">Tagging options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Suggested tags with confidence scores.</returns>
    Task<TagSuggestionResult> SuggestTagsForContentItemAsync(
        Guid contentItemGuid,
        TaggingOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies suggested tags to a content item.
    /// </summary>
    /// <param name="contentItemGuid">Content item GUID.</param>
    /// <param name="tagGuids">Tag GUIDs to apply.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of the tagging operation.</returns>
    Task<TagApplicationResult> ApplyTagsAsync(
        Guid contentItemGuid,
        IEnumerable<Guid> tagGuids,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Auto-tags content item (suggests and applies in one operation).
    /// </summary>
    /// <param name="contentItemGuid">Content item GUID.</param>
    /// <param name="options">Tagging options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of the auto-tagging operation.</returns>
    Task<AutoTagResult> AutoTagContentAsync(
        Guid contentItemGuid,
        TaggingOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk auto-tags multiple content items.
    /// </summary>
    /// <param name="contentItemGuids">Content item GUIDs.</param>
    /// <param name="options">Tagging options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Results for each content item.</returns>
    Task<IReadOnlyList<AutoTagResult>> BulkAutoTagAsync(
        IEnumerable<Guid> contentItemGuids,
        TaggingOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available taxonomies for auto-tagging.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Available taxonomy information.</returns>
    Task<IReadOnlyList<TaxonomyInfo>> GetAvailableTaxonomiesAsync(
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for taxonomy embedding management.
/// Manages embeddings for taxonomy tags to enable semantic matching.
/// </summary>
public interface ITaxonomyEmbeddingService
{
    /// <summary>
    /// Builds embeddings for all tags in a taxonomy.
    /// </summary>
    /// <param name="taxonomyName">Taxonomy code name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task BuildTaxonomyEmbeddingsAsync(
        string taxonomyName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds embeddings for all configured taxonomies.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RebuildAllTaxonomyEmbeddingsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds tags semantically similar to the given text.
    /// </summary>
    /// <param name="text">Text to match.</param>
    /// <param name="taxonomyName">Optional taxonomy filter.</param>
    /// <param name="topK">Number of results.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Similar tags with scores.</returns>
    Task<IReadOnlyList<TagMatch>> FindSimilarTagsAsync(
        string text,
        string? taxonomyName = null,
        int topK = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets embedding for a specific tag.
    /// </summary>
    /// <param name="tagGuid">Tag GUID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tag embedding or null if not found.</returns>
    Task<float[]?> GetTagEmbeddingAsync(
        Guid tagGuid,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Content to be analyzed for tagging.
/// </summary>
public sealed class ContentToTag
{
    /// <summary>
    /// Content title.
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// Content body/description.
    /// </summary>
    public string Body { get; init; } = string.Empty;

    /// <summary>
    /// Additional metadata for context.
    /// </summary>
    public IDictionary<string, string>? Metadata { get; init; }

    /// <summary>
    /// Content type for filtering applicable taxonomies.
    /// </summary>
    public string? ContentType { get; init; }
}

/// <summary>
/// Options for content tagging.
/// </summary>
public sealed class TaggingOptions
{
    /// <summary>
    /// Specific taxonomy names to use. If null, uses all configured taxonomies.
    /// </summary>
    public IEnumerable<string>? TaxonomyNames { get; init; }

    /// <summary>
    /// Minimum confidence score to include a tag (0-1). Default: 0.7.
    /// </summary>
    public double MinConfidence { get; init; } = 0.7;

    /// <summary>
    /// Maximum number of tags to suggest per taxonomy. Default: 5.
    /// </summary>
    public int MaxTagsPerTaxonomy { get; init; } = 5;

    /// <summary>
    /// Whether to use LLM for tag suggestion (vs pure embedding similarity).
    /// </summary>
    public bool UseLLM { get; init; } = true;

    /// <summary>
    /// Whether to include tag descriptions in matching.
    /// </summary>
    public bool IncludeDescriptions { get; init; } = true;

    /// <summary>
    /// Whether to auto-apply suggestions above confidence threshold.
    /// </summary>
    public bool AutoApply { get; init; }

    /// <summary>
    /// Culture for localized tag matching.
    /// </summary>
    public string? CultureCode { get; init; }
}

/// <summary>
/// Result of tag suggestion.
/// </summary>
public sealed class TagSuggestionResult
{
    /// <summary>
    /// Whether the operation was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Suggested tags grouped by taxonomy.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<SuggestedTag>> TagsByTaxonomy { get; init; }
        = new Dictionary<string, IReadOnlyList<SuggestedTag>>();

    /// <summary>
    /// All suggested tags flattened.
    /// </summary>
    public IReadOnlyList<SuggestedTag> AllTags { get; init; } = [];

    /// <summary>
    /// Error message if unsuccessful.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Processing time in milliseconds.
    /// </summary>
    public long ProcessingTimeMs { get; init; }
}

/// <summary>
/// A suggested tag with confidence score.
/// </summary>
public sealed class SuggestedTag
{
    /// <summary>
    /// Tag GUID.
    /// </summary>
    public Guid TagGuid { get; init; }

    /// <summary>
    /// Tag name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Taxonomy name.
    /// </summary>
    public string TaxonomyName { get; init; } = string.Empty;

    /// <summary>
    /// Confidence score (0-1).
    /// </summary>
    public double Confidence { get; init; }

    /// <summary>
    /// Reason for suggestion (from LLM).
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// Whether already applied to content.
    /// </summary>
    public bool IsAlreadyApplied { get; init; }
}

/// <summary>
/// Result of applying tags.
/// </summary>
public sealed class TagApplicationResult
{
    /// <summary>
    /// Whether the operation was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Number of tags applied.
    /// </summary>
    public int AppliedCount { get; init; }

    /// <summary>
    /// Number of tags already present.
    /// </summary>
    public int AlreadyPresentCount { get; init; }

    /// <summary>
    /// Error message if unsuccessful.
    /// </summary>
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Result of auto-tagging operation.
/// </summary>
public sealed class AutoTagResult
{
    /// <summary>
    /// Content item GUID.
    /// </summary>
    public Guid ContentItemGuid { get; init; }

    /// <summary>
    /// Whether the operation was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Suggested tags.
    /// </summary>
    public IReadOnlyList<SuggestedTag> SuggestedTags { get; init; } = [];

    /// <summary>
    /// Applied tags (if AutoApply was true).
    /// </summary>
    public IReadOnlyList<SuggestedTag> AppliedTags { get; init; } = [];

    /// <summary>
    /// Error message if unsuccessful.
    /// </summary>
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Tag match result from semantic search.
/// </summary>
public sealed class TagMatch
{
    /// <summary>
    /// Tag GUID.
    /// </summary>
    public Guid TagGuid { get; init; }

    /// <summary>
    /// Tag name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Taxonomy name.
    /// </summary>
    public string TaxonomyName { get; init; } = string.Empty;

    /// <summary>
    /// Similarity score (0-1).
    /// </summary>
    public double Score { get; init; }
}

/// <summary>
/// Taxonomy information.
/// </summary>
public sealed class TaxonomyInfo
{
    /// <summary>
    /// Taxonomy name (code name).
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Display name.
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    /// Number of tags.
    /// </summary>
    public int TagCount { get; init; }

    /// <summary>
    /// Whether embeddings are built.
    /// </summary>
    public bool HasEmbeddings { get; init; }
}
