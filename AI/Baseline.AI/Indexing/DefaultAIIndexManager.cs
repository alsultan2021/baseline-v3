using System.Security.Cryptography;
using System.Text;

using Baseline.AI.Data;
using Baseline.AI.Security;
using Baseline.AI.Services;

using CMS.DataEngine;

using Microsoft.Extensions.Logging;

namespace Baseline.AI.Indexing;

/// <summary>
/// Default implementation of IAIIndexManager - orchestrates indexing operations.
/// </summary>
public class DefaultAIIndexManager : IAIIndexManager
{
    private readonly IAIContentScanner _contentScanner;
    private readonly IAIStrategyRegistry _strategyRegistry;
    private readonly IAIChunkingService _chunkingService;
    private readonly IAIEmbeddingService _embeddingService;
    private readonly IVectorStore _vectorStore;
    private readonly ILogger<DefaultAIIndexManager> _logger;

    // Lazy providers to avoid blocking startup with Provider<T>.Instance
    private IInfoProvider<AIKnowledgeBaseInfo> KbProvider => Provider<AIKnowledgeBaseInfo>.Instance;
    private IInfoProvider<AIIndexQueueInfo> QueueProvider => Provider<AIIndexQueueInfo>.Instance;
    private IInfoProvider<AIRebuildJobInfo> JobProvider => Provider<AIRebuildJobInfo>.Instance;
    private IInfoProvider<AIContentFingerprintInfo> FingerprintProvider => Provider<AIContentFingerprintInfo>.Instance;

    public DefaultAIIndexManager(
        IAIContentScanner contentScanner,
        IAIStrategyRegistry strategyRegistry,
        IAIChunkingService chunkingService,
        IAIEmbeddingService embeddingService,
        IVectorStore vectorStore,
        ILogger<DefaultAIIndexManager> logger)
    {
        _contentScanner = contentScanner;
        _strategyRegistry = strategyRegistry;
        _chunkingService = chunkingService;
        _embeddingService = embeddingService;
        _vectorStore = vectorStore;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<int> StartRebuildAsync(int knowledgeBaseId, CancellationToken cancellationToken = default)
    {
        var kb = (await KbProvider.Get().WhereEquals(nameof(AIKnowledgeBaseInfo.KnowledgeBaseId), knowledgeBaseId).TopN(1).GetEnumerableTypedResultAsync()).FirstOrDefault();
        if (kb is null)
        {
            throw new ArgumentException($"Knowledge base with ID {knowledgeBaseId} not found.", nameof(knowledgeBaseId));
        }

        var strategy = _strategyRegistry.GetStrategy(kb.KnowledgeBaseStrategyName);
        if (strategy is null)
        {
            throw new InvalidOperationException($"Strategy '{kb.KnowledgeBaseStrategyName}' not found.");
        }

        // Create rebuild job
        var job = new AIRebuildJobInfo
        {
            JobKnowledgeBaseId = knowledgeBaseId,
            JobStatus = 1, // Rebuilding status
            JobStarted = DateTime.UtcNow,
            JobScannedCount = 0,
            JobTotalCount = 0,
            JobChunkCount = 0,
            JobFailedCount = 0
        };
        JobProvider.Set(job);

        try
        {
            _logger.LogInformation("Starting rebuild for KB {KnowledgeBaseId}", knowledgeBaseId);

            // Update KB status
            kb.KnowledgeBaseStatus = (int)KnowledgeBaseStatus.Rebuilding;
            kb.KnowledgeBaseLastError = null;
            KbProvider.Set(kb);

            // Delete existing vectors
            await _vectorStore.DeleteByKnowledgeBaseAsync(knowledgeBaseId, cancellationToken);
            _logger.LogDebug("Deleted existing vectors for KB {KnowledgeBaseId}", knowledgeBaseId);

            // Scan all content
            var items = new List<IAIIndexableItem>();
            await foreach (var item in _contentScanner.ScanAllAsync(knowledgeBaseId, cancellationToken))
            {
                items.Add(item);
            }
            job.JobTotalCount = items.Count;
            JobProvider.Set(job);

            _logger.LogInformation("Scanned {Count} items for KB {KnowledgeBaseId}", items.Count, knowledgeBaseId);

            int totalChunks = 0;
            int errors = 0;

            // Process each item
            foreach (var item in items)
            {
                if (cancellationToken.IsCancellationRequested) break;

                try
                {
                    var chunks = await IndexItemAsync(knowledgeBaseId, item, cancellationToken);
                    totalChunks += chunks;
                    job.JobScannedCount++;
                    job.JobChunkCount = totalChunks;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error indexing item {Guid} for KB {KnowledgeBaseId}", item.ContentItemGuid, knowledgeBaseId);
                    errors++;
                    job.JobFailedCount++;
                }

                // Update job progress every 10 items
                if (job.JobScannedCount % 10 == 0)
                {
                    JobProvider.Set(job);
                }
            }

            // Finalize job
            job.JobFinished = DateTime.UtcNow;
            job.JobStatus = errors > 0 ? 2 : 3; // 2=Failed, 3=Completed
            JobProvider.Set(job);

            // Update KB status and stats
            kb.KnowledgeBaseStatus = errors > 0 ? (int)KnowledgeBaseStatus.PartiallyBuilt : (int)KnowledgeBaseStatus.Idle;
            kb.KnowledgeBaseLastRebuild = DateTime.UtcNow;
            kb.KnowledgeBaseDocumentCount = job.JobScannedCount;
            kb.KnowledgeBaseChunkCount = totalChunks;
            kb.KnowledgeBaseStrategyHash = strategy.ComputeStrategyHash();
            KbProvider.Set(kb);

            _logger.LogInformation("Rebuild completed for KB {KnowledgeBaseId}: {Items} items, {Chunks} chunks, {Errors} errors",
                knowledgeBaseId, job.JobScannedCount, totalChunks, errors);

            return job.JobId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Rebuild failed for KB {KnowledgeBaseId}", knowledgeBaseId);

            job.JobStatus = 2; // Failed
            job.JobFinished = DateTime.UtcNow;
            JobProvider.Set(job);

            kb.KnowledgeBaseStatus = (int)KnowledgeBaseStatus.Failed;
            kb.KnowledgeBaseLastError = ex.Message;
            KbProvider.Set(kb);

            throw;
        }
    }

    /// <inheritdoc />
    public async Task<int> ProcessQueueAsync(
        int? knowledgeBaseId = null,
        int batchSize = 100,
        CancellationToken cancellationToken = default)
    {
        // Check if AI tables exist before querying
        try
        {
            var query = QueueProvider.Get()
                .OrderBy(nameof(AIIndexQueueInfo.QueueCreated));

            if (knowledgeBaseId.HasValue)
            {
                query = query.WhereEquals(nameof(AIIndexQueueInfo.QueueKnowledgeBaseId), knowledgeBaseId.Value);
            }

            var queueItems = (await query.TopN(batchSize).GetEnumerableTypedResultAsync()).ToList();

            if (queueItems.Count == 0)
            {
                return 0;
            }

            _logger.LogDebug("Processing {Count} queue items", queueItems.Count);

            int processed = 0;

            foreach (var queueItem in queueItems)
            {
                if (cancellationToken.IsCancellationRequested) break;

                try
                {
                    var kb = (await KbProvider.Get().WhereEquals(nameof(AIKnowledgeBaseInfo.KnowledgeBaseId), queueItem.QueueKnowledgeBaseId).TopN(1).GetEnumerableTypedResultAsync()).FirstOrDefault();
                    if (kb is null)
                    {
                        _logger.LogWarning("Knowledge base {KbId} not found for queue item {QueueId}", queueItem.QueueKnowledgeBaseId, queueItem.QueueId);
                        continue;
                    }

                    if (queueItem.QueueOperationType == (int)IndexOperationType.Reconcile)
                    {
                        // Get the item and check if it matches configuration
                        var item = await _contentScanner.GetItemAsync(
                            queueItem.QueueContentItemGuid,
                            queueItem.QueueLanguageCode,
                            queueItem.QueueChannelId ?? 0,
                            cancellationToken);

                        if (item is not null && _contentScanner.MatchesConfiguration(queueItem.QueueKnowledgeBaseId, item.ContentTypeName, item.ChannelId, item.UrlPath))
                        {
                            // Matches - index it
                            await IndexItemAsync(queueItem.QueueKnowledgeBaseId, item, cancellationToken);
                        }
                        else
                        {
                            // Doesn't match or doesn't exist - delete any existing vectors
                            await DeleteItemAsync(
                                queueItem.QueueKnowledgeBaseId,
                                queueItem.QueueContentItemGuid,
                                queueItem.QueueLanguageCode,
                                queueItem.QueueChannelId,
                                cancellationToken);
                        }
                    }
                    else if (queueItem.QueueOperationType == (int)IndexOperationType.Delete)
                    {
                        await DeleteItemAsync(
                            queueItem.QueueKnowledgeBaseId,
                            queueItem.QueueContentItemGuid,
                            queueItem.QueueLanguageCode,
                            queueItem.QueueChannelId,
                            cancellationToken);
                    }

                    // Delete processed queue item
                    QueueProvider.Delete(queueItem);
                    processed++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing queue item {QueueId}", queueItem.QueueId);
                    // queueItem.QueueStatus = "Failed" - property doesn't exist
                    QueueProvider.Set(queueItem);
                }
            }

            _logger.LogInformation("Processed {Processed} queue items", processed);
            return processed;
        }
        catch (Exception ex) when (
            ex.Message.Contains("Invalid object name") ||
            ex.Message.Contains("connection is closed") ||
            ex.Message.Contains("has been registered") ||
            ex.InnerException?.Message.Contains("has been registered") == true)
        {
            // AI tables/types don't exist yet - installers haven't run
            _logger.LogDebug("AI index queue not ready yet (types not registered), skipping processing");
            return 0;
        }
    }

    /// <inheritdoc />
    public async Task<int> IndexItemAsync(
        int knowledgeBaseId,
        IAIIndexableItem item,
        CancellationToken cancellationToken = default)
    {
        var kb = (await KbProvider.Get().WhereEquals(nameof(AIKnowledgeBaseInfo.KnowledgeBaseId), knowledgeBaseId).TopN(1).GetEnumerableTypedResultAsync()).FirstOrDefault();
        if (kb is null)
        {
            throw new ArgumentException($"Knowledge base with ID {knowledgeBaseId} not found.", nameof(knowledgeBaseId));
        }

        var strategy = _strategyRegistry.GetStrategy(kb.KnowledgeBaseStrategyName);
        if (strategy is null)
        {
            throw new InvalidOperationException($"Strategy '{kb.KnowledgeBaseStrategyName}' not found.");
        }

        // Extract content
        var extracted = await strategy.ExtractAsync(item, cancellationToken);
        if (extracted is null || string.IsNullOrWhiteSpace(extracted.Content))
        {
            _logger.LogDebug("No content extracted for item {Guid}, deleting existing vectors", item.ContentItemGuid);
            await DeleteItemAsync(knowledgeBaseId, item.ContentItemGuid, item.LanguageCode, item.ChannelId, cancellationToken);
            return 0;
        }

        // Sanitize content for security
        var sanitizedContent = ContentSanitizer.Sanitize(extracted.Content);
        if (ContentSanitizer.ContainsSuspiciousPatterns(extracted.Content))
        {
            var patterns = ContentSanitizer.GetDetectedPatterns(extracted.Content);
            _logger.LogWarning("Suspicious patterns detected in item {Guid}: {Patterns}", item.ContentItemGuid, string.Join(", ", patterns));
        }

        // Compute fingerprint (after sanitization)
        var fingerprint = ComputeFingerprint(sanitizedContent);

        // Check if content changed
        var existingFingerprint = (await FingerprintProvider.Get()
            .WhereEquals(nameof(AIContentFingerprintInfo.FingerprintKnowledgeBaseId), knowledgeBaseId)
            .WhereEquals(nameof(AIContentFingerprintInfo.FingerprintContentGuid), item.ContentItemGuid)
            .WhereEquals(nameof(AIContentFingerprintInfo.FingerprintLanguageCode), item.LanguageCode)
            .WhereEquals(nameof(AIContentFingerprintInfo.FingerprintChannelId), item.ChannelId)
            .TopN(1)
            .GetEnumerableTypedResultAsync()).FirstOrDefault();

        if (existingFingerprint is not null && existingFingerprint.FingerprintHash == fingerprint)
        {
            _logger.LogDebug("Content unchanged for item {Guid}, skipping indexing", item.ContentItemGuid);
            return 0; // No-op optimization
        }

        // Get chunking options and chunk the sanitized content
        var chunkingOptions = strategy.GetChunkingOptions();
        var chunks = _chunkingService.ChunkContent(sanitizedContent, chunkingOptions);

        if (chunks.Count == 0)
        {
            _logger.LogWarning("No chunks generated for item {Guid}", item.ContentItemGuid);
            return 0;
        }

        _logger.LogDebug("Generated {Count} chunks for item {Guid}", chunks.Count, item.ContentItemGuid);

        // Generate embeddings for all chunks
        var chunkContents = chunks.Select(c => c.Content).ToList();
        var embeddings = await _embeddingService.GenerateEmbeddingsAsync(chunkContents, cancellationToken);

        if (embeddings.Count != chunks.Count)
        {
            throw new InvalidOperationException($"Embedding count ({embeddings.Count}) doesn't match chunk count ({chunks.Count})");
        }

        // Create documents with embeddings for vector store
        var documentsWithEmbeddings = new List<(AIDocument Document, float[] Embedding)>();
        for (int i = 0; i < chunks.Count; i++)
        {
            var chunk = chunks[i];
            var embedding = embeddings[i];

            var docId = $"kb:{knowledgeBaseId}:{item.ContentItemGuid}:{extracted.LanguageCode}:{extracted.ChannelId}:{i}";

            var document = new AIDocument
            {
                Id = docId,
                ContentItemId = item.ContentItemId,
                ContentItemGuid = item.ContentItemGuid,
                Content = chunk.Content,
                Title = extracted.Title,
                Url = extracted.Url,
                ContentTypeName = extracted.ContentTypeName,
                LanguageCode = extracted.LanguageCode,
                LastModified = extracted.LastModified,
                Metadata = new Dictionary<string, object>
                {
                    ["KnowledgeBaseId"] = knowledgeBaseId,
                    ["ChannelId"] = extracted.ChannelId,
                    ["ChannelName"] = extracted.ChannelName ?? "",
                    ["UrlPath"] = extracted.UrlPath ?? "",
                    ["ChunkIndex"] = i,
                    ["TotalChunks"] = chunks.Count,
                    ["ContentFingerprint"] = fingerprint
                }
            };

            documentsWithEmbeddings.Add((document, embedding));
        }

        // Store vectors (upsert - will replace existing)
        await _vectorStore.UpsertBatchAsync(documentsWithEmbeddings, cancellationToken);
        _logger.LogDebug("Stored {Count} vector records for item {Guid}", documentsWithEmbeddings.Count, item.ContentItemGuid);

        // Update fingerprint
        if (existingFingerprint is null)
        {
            existingFingerprint = new AIContentFingerprintInfo
            {
                FingerprintKnowledgeBaseId = knowledgeBaseId,
                FingerprintContentGuid = item.ContentItemGuid,
                FingerprintLanguageCode = item.LanguageCode,
                FingerprintChannelId = item.ChannelId,
                FingerprintHash = fingerprint,
                FingerprintLastChecked = DateTime.UtcNow
            };
        }
        else
        {
            existingFingerprint.FingerprintHash = fingerprint;
            existingFingerprint.FingerprintLastChecked = DateTime.UtcNow;
        }
        FingerprintProvider.Set(existingFingerprint);

        return chunks.Count;
    }

    /// <inheritdoc />
    public async Task DeleteItemAsync(
        int knowledgeBaseId,
        Guid contentItemGuid,
        string? languageCode = null,
        int? channelId = null,
        CancellationToken cancellationToken = default)
    {
        // Delete vectors
        await _vectorStore.DeleteByItemAsync(knowledgeBaseId, contentItemGuid, languageCode, channelId, cancellationToken);

        // Delete fingerprints
        var fingerprintQuery = FingerprintProvider.Get()
            .WhereEquals(nameof(AIContentFingerprintInfo.FingerprintKnowledgeBaseId), knowledgeBaseId)
            .WhereEquals(nameof(AIContentFingerprintInfo.FingerprintContentGuid), contentItemGuid);

        if (languageCode is not null)
        {
            fingerprintQuery = fingerprintQuery.WhereEquals(nameof(AIContentFingerprintInfo.FingerprintLanguageCode), languageCode);
        }

        if (channelId.HasValue)
        {
            fingerprintQuery = fingerprintQuery.WhereEquals(nameof(AIContentFingerprintInfo.FingerprintChannelId), channelId.Value);
        }

        var fingerprints = (await fingerprintQuery.GetEnumerableTypedResultAsync()).ToList();
        foreach (var fingerprint in fingerprints)
        {
            FingerprintProvider.Delete(fingerprint);
        }

        _logger.LogDebug("Deleted vectors and fingerprints for item {Guid} in KB {KnowledgeBaseId}", contentItemGuid, knowledgeBaseId);
    }

    /// <inheritdoc />
    public async Task<RebuildJobStatus?> GetJobStatusAsync(int jobId, CancellationToken cancellationToken = default)
    {
        var job = (await JobProvider.Get().WhereEquals(nameof(AIRebuildJobInfo.JobId), jobId).TopN(1).GetEnumerableTypedResultAsync()).FirstOrDefault();
        if (job is null)
        {
            return null;
        }

        return new RebuildJobStatus(
            job.JobId,
            job.JobKnowledgeBaseId,
            job.JobStatus.ToString(),
            job.JobScannedCount,
            job.JobTotalCount,
            job.JobChunkCount,
            job.JobFailedCount,
            job.JobStarted,
            job.JobFinished);
    }

    /// <inheritdoc />
    public async Task QueueItemAsync(
        int knowledgeBaseId,
        Guid contentItemGuid,
        int channelId,
        IndexOperationType operationType,
        string languageCode = "en",
        CancellationToken cancellationToken = default)
    {
        var queueItem = new AIIndexQueueInfo
        {
            QueueKnowledgeBaseId = knowledgeBaseId,
            QueueContentItemGuid = contentItemGuid,
            QueueChannelId = channelId,
            QueueLanguageCode = languageCode,
            QueueOperationType = (int)operationType,
            QueueCreated = DateTime.UtcNow
        };

        QueueProvider.Set(queueItem);
        _logger.LogDebug("Queued {Operation} for item {Guid} (lang: {Lang}) in KB {KnowledgeBaseId}", operationType, contentItemGuid, languageCode, knowledgeBaseId);

        await Task.CompletedTask;
    }

    private static string ComputeFingerprint(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
