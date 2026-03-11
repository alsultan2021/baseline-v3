namespace Baseline.AI.Indexing;

/// <summary>
/// Orchestrates AI indexing operations including rebuild and incremental updates.
/// </summary>
public interface IAIIndexManager
{
    /// <summary>
    /// Starts a full rebuild of a knowledge base.
    /// Clears existing vectors and re-indexes all matching content.
    /// </summary>
    /// <param name="knowledgeBaseId">The knowledge base ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The rebuild job ID.</returns>
    Task<int> StartRebuildAsync(int knowledgeBaseId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes pending items in the incremental update queue.
    /// </summary>
    /// <param name="knowledgeBaseId">Knowledge base ID, or null for all.</param>
    /// <param name="batchSize">Maximum items to process.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of items processed.</returns>
    Task<int> ProcessQueueAsync(
        int? knowledgeBaseId = null,
        int batchSize = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Indexes a single content item.
    /// </summary>
    /// <param name="knowledgeBaseId">The knowledge base ID.</param>
    /// <param name="item">The item to index.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of chunks indexed.</returns>
    Task<int> IndexItemAsync(
        int knowledgeBaseId,
        IAIIndexableItem item,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a content item from the index.
    /// </summary>
    /// <param name="knowledgeBaseId">The knowledge base ID.</param>
    /// <param name="contentItemGuid">Content item GUID.</param>
    /// <param name="languageCode">Language code, or null for all languages.</param>
    /// <param name="channelId">Channel ID, or null for all channels.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteItemAsync(
        int knowledgeBaseId,
        Guid contentItemGuid,
        string? languageCode = null,
        int? channelId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the status of a rebuild job.
    /// </summary>
    /// <param name="jobId">The job ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Job status, or null if not found.</returns>
    Task<RebuildJobStatus?> GetJobStatusAsync(int jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Queues a content item for incremental update.
    /// </summary>
    /// <param name="knowledgeBaseId">The knowledge base ID.</param>
    /// <param name="contentItemGuid">Content item GUID.</param>
    /// <param name="channelId">Channel ID (0 for reusable).</param>
    /// <param name="operationType">Type of operation.</param>
    /// <param name="languageCode">Language code (e.g., "en").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task QueueItemAsync(
        int knowledgeBaseId,
        Guid contentItemGuid,
        int channelId,
        IndexOperationType operationType,
        string languageCode = "en",
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Status information for a rebuild job.
/// </summary>
public sealed record RebuildJobStatus(
    int JobId,
    int KnowledgeBaseId,
    string Status,
    int ScannedCount,
    int TotalCount,
    int ChunkCount,
    int FailedCount,
    DateTime StartedAt,
    DateTime? CompletedAt);
