namespace Baseline.AI.Indexing;

/// <summary>
/// Scans content items for indexing based on knowledge base configuration.
/// </summary>
public interface IAIContentScanner
{
    /// <summary>
    /// Scans all content matching a knowledge base configuration.
    /// Used for full rebuild operations.
    /// </summary>
    /// <param name="knowledgeBaseId">The knowledge base ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Stream of indexable items.</returns>
    IAsyncEnumerable<IAIIndexableItem> ScanAllAsync(
        int knowledgeBaseId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific content item for indexing.
    /// </summary>
    /// <param name="contentItemGuid">The content item GUID.</param>
    /// <param name="languageCode">The language code.</param>
    /// <param name="channelId">Channel ID (0 for reusable, >0 for page).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The indexable item, or null if not found.</returns>
    Task<IAIIndexableItem?> GetItemAsync(
        Guid contentItemGuid,
        string languageCode,
        int channelId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a content item matches the knowledge base configuration.
    /// </summary>
    /// <param name="knowledgeBaseId">The knowledge base ID.</param>
    /// <param name="contentTypeName">Content type name.</param>
    /// <param name="channelId">Channel ID (0 for reusable).</param>
    /// <param name="urlPath">URL path (for page items).</param>
    /// <returns>True if the item should be indexed.</returns>
    bool MatchesConfiguration(
        int knowledgeBaseId,
        string contentTypeName,
        int channelId,
        string? urlPath);
}
