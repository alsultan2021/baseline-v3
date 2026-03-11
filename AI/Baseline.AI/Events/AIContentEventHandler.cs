using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Websites;
using CMS.Websites.Internal;

using Baseline.AI.Data;
using Baseline.AI.Indexing;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using IndexOperationType = Baseline.AI.Indexing.IndexOperationType;
using ContentItemInfo = CMS.ContentEngine.Internal.ContentItemInfo;

namespace Baseline.AI.Events;

/// <summary>
/// Handles content events to queue AI indexing operations.
/// Monitors publish, unpublish, delete, and move events.
/// </summary>
public class AIContentEventHandler
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AIContentEventHandler> _logger;

    public AIContentEventHandler(
        IServiceProvider serviceProvider,
        ILogger<AIContentEventHandler> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Registers all content event handlers.
    /// Called from module initialization.
    /// </summary>
    public void Register()
    {
        ContentItemEvents.Publish.Execute += OnContentItemPublished;
        ContentItemEvents.Unpublish.Execute += OnContentItemUnpublished;
        ContentItemEvents.Delete.Execute += OnContentItemDeleted;
        WebPageEvents.Move.Execute += OnWebPageMoved;

        _logger.LogDebug("AI content event handlers registered");
    }

    /// <summary>
    /// Unregisters all content event handlers.
    /// </summary>
    public void Unregister()
    {
        ContentItemEvents.Publish.Execute -= OnContentItemPublished;
        ContentItemEvents.Unpublish.Execute -= OnContentItemUnpublished;
        ContentItemEvents.Delete.Execute -= OnContentItemDeleted;
        WebPageEvents.Move.Execute -= OnWebPageMoved;
    }

    private void OnContentItemPublished(object? sender, PublishContentItemEventArgs e)
    {
        // Capture all data from event args synchronously — zero DB calls.
        var contentItemId = e.ID;
        var contentItemGuid = e.Guid;
        if (contentItemId <= 0) return;

        // Suppress ExecutionContext flow so Task.Run does NOT inherit
        // Kentico's ambient CMSConnectionScope (transactional DB connection).
        var restoreFlow = ExecutionContext.SuppressFlow();
        _ = Task.Run(async () =>
        {
            try
            {
                await QueueReconcileByGuid(contentItemGuid, IndexOperationType.Reconcile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling ContentItemPublished event for item {ItemId}", contentItemId);
            }
        });
        restoreFlow.Undo();
    }

    private void OnContentItemUnpublished(object? sender, UnpublishContentItemEventArgs e)
    {
        var contentItemId = e.ID;
        var contentItemGuid = e.Guid;
        if (contentItemId <= 0) return;

        var restoreFlow = ExecutionContext.SuppressFlow();
        _ = Task.Run(async () =>
        {
            try
            {
                await QueueReconcileByGuid(contentItemGuid, IndexOperationType.Delete);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling ContentItemUnpublished event for item {ItemId}", contentItemId);
            }
        });
        restoreFlow.Undo();
    }

    private void OnContentItemDeleted(object? sender, DeleteContentItemEventArgs e)
    {
        // Capture GUID from args — item won't exist in DB after delete.
        var contentItemId = e.ID;
        var contentItemGuid = e.Guid;
        if (contentItemId <= 0) return;

        var restoreFlow = ExecutionContext.SuppressFlow();
        _ = Task.Run(async () =>
        {
            try
            {
                await QueueReconcileByGuid(contentItemGuid, IndexOperationType.Delete);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling ContentItemDeleted event for item {ItemId}", contentItemId);
            }
        });
        restoreFlow.Undo();
    }

    private void OnWebPageMoved(object? sender, MoveWebPageEventArgs e)
    {
        var webPageItemId = e.ID;
        if (webPageItemId <= 0) return;

        var restoreFlow = ExecutionContext.SuppressFlow();
        _ = Task.Run(async () =>
        {
            try
            {
                await QueueReconcileForWebPage(webPageItemId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling WebPageMoved event for page {PageId}", webPageItemId);
            }
        });
        restoreFlow.Undo();
    }

    /// <summary>
    /// Queues AI indexing by GUID — zero DB calls in event handler (Lucene pattern).
    /// All DB work happens inside a fresh DI scope with its own connection.
    /// </summary>
    private async Task QueueReconcileByGuid(Guid contentItemGuid, IndexOperationType operationType)
    {
        if (contentItemGuid == Guid.Empty) return;

        using var scope = _serviceProvider.CreateScope();
        var sp = scope.ServiceProvider;

        var kbProvider = sp.GetRequiredService<IInfoProvider<AIKnowledgeBaseInfo>>();
        var knowledgeBases = kbProvider.Get().GetEnumerableTypedResult().ToList();

        foreach (var kb in knowledgeBases)
        {
            if (kb.KnowledgeBaseStatus == (int)KnowledgeBaseStatus.Rebuilding)
            {
                continue;
            }

            try
            {
                var indexManager = sp.GetService<IAIIndexManager>();
                if (indexManager is null) continue;

                // Enqueue for all configured languages in this KB
                var languages = kb.KnowledgeBaseLanguages.Trim('[', ']')
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim('"', ' '))
                    .Where(s => !string.IsNullOrWhiteSpace(s));

                foreach (var lang in languages)
                {
                    await indexManager.QueueItemAsync(
                        kb.KnowledgeBaseId,
                        contentItemGuid,
                        channelId: 0, // Will be determined during reconcile
                        operationType);
                }

                _logger.LogDebug("Queued {Operation} for content item {Guid} in KB {KbId}",
                    operationType, contentItemGuid, kb.KnowledgeBaseId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error queuing {Operation} for item {Guid} in KB {KbId}",
                    operationType, contentItemGuid, kb.KnowledgeBaseId);
            }
        }
    }

    private async Task QueueReconcileForWebPage(int webPageItemId)
    {
        using var scope = _serviceProvider.CreateScope();
        var sp = scope.ServiceProvider;

        var kbProvider = sp.GetRequiredService<IInfoProvider<AIKnowledgeBaseInfo>>();
        var knowledgeBases = kbProvider.Get().GetEnumerableTypedResult().ToList();

        // Resolve web page item to content item GUID
        var webPageProvider = sp.GetRequiredService<IInfoProvider<WebPageItemInfo>>();
        var webPageItem = webPageProvider.Get()
            .WhereEquals("WebPageItemID", webPageItemId)
            .TopN(1)
            .FirstOrDefault();

        if (webPageItem is null)
        {
            _logger.LogDebug("Web page item {Id} not found, skipping queue", webPageItemId);
            return;
        }

        var contentItemId = (int)webPageItem.GetValue("WebPageItemContentItemID");

        var contentItemProvider = sp.GetRequiredService<IInfoProvider<ContentItemInfo>>();
        var contentItem = contentItemProvider.Get()
            .WhereEquals("ContentItemID", contentItemId)
            .TopN(1)
            .FirstOrDefault();

        if (contentItem is null)
        {
            _logger.LogDebug("Content item for web page {Id} not found, skipping queue", webPageItemId);
            return;
        }

        var contentItemGuid = (Guid)contentItem.GetValue("ContentItemGUID");
        var channelId = (int)webPageItem.GetValue("WebPageItemWebsiteChannelID");

        foreach (var kb in knowledgeBases)
        {
            if (kb.KnowledgeBaseStatus == (int)KnowledgeBaseStatus.Rebuilding)
            {
                continue;
            }

            try
            {
                var indexManager = sp.GetService<IAIIndexManager>();
                if (indexManager is null) continue;

                await indexManager.QueueItemAsync(
                    kb.KnowledgeBaseId,
                    contentItemGuid,
                    channelId,
                    IndexOperationType.Reconcile);

                _logger.LogDebug("Queued Reconcile for webpage {Id} (GUID: {Guid}) in KB {KbId}",
                    webPageItemId, contentItemGuid, kb.KnowledgeBaseId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error queuing Reconcile for webpage {Id} in KB {KbId}",
                    webPageItemId, kb.KnowledgeBaseId);
            }
        }
    }
}
