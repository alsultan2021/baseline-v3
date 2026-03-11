using System.Threading;
using CMS.ContentEngine;
using CMS.ContentEngine.Internal;
using CMS.DataEngine;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baseline.Localization.Events;

/// <summary>
/// Subscribes to <see cref="ContentItemEvents.Publish"/> to detect content items
/// missing translations in configured languages — "translation workflow webhooks."
/// Runs detection on a fire-and-forget background thread to avoid blocking the publish pipeline.
/// </summary>
public sealed class TranslationWorkflowEventHandler
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TranslationWorkflowEventHandler> _logger;

    public TranslationWorkflowEventHandler(
        IServiceProvider serviceProvider,
        ILogger<TranslationWorkflowEventHandler> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Registers event handlers. Called from module initialization.
    /// </summary>
    public void Register()
    {
        ContentItemEvents.Publish.Execute += OnContentItemPublished;
        _logger.LogDebug("Translation workflow event handlers registered");
    }

    /// <summary>
    /// Unregisters event handlers.
    /// </summary>
    public void Unregister()
    {
        ContentItemEvents.Publish.Execute -= OnContentItemPublished;
    }

    private void OnContentItemPublished(object? sender, PublishContentItemEventArgs e)
    {
        var contentItemId = e.ID;
        if (contentItemId <= 0)
        {
            return;
        }

        // Suppress ExecutionContext flow so Task.Run gets its own DB connection
        var restoreFlow = ExecutionContext.SuppressFlow();
        _ = Task.Run(async () =>
        {
            try
            {
                await DetectMissingTranslationsAsync(contentItemId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting missing translations for item {ItemId}", contentItemId);
            }
        });
        restoreFlow.Undo();
    }

    private async Task DetectMissingTranslationsAsync(int contentItemId)
    {
        using var scope = _serviceProvider.CreateScope();
        var sp = scope.ServiceProvider;

        var opts = sp.GetService<IOptions<BaselineLocalizationOptions>>()?.Value;
        if (opts is null || !opts.EnableTranslationWorkflow)
        {
            return;
        }

        var languageProvider = sp.GetRequiredService<IInfoProvider<ContentLanguageInfo>>();
        var metadataProvider = sp.GetRequiredService<IInfoProvider<ContentItemLanguageMetadataInfo>>();
        var contentItemProvider = sp.GetRequiredService<IInfoProvider<ContentItemInfo>>();

        // Resolve the content item GUID / name for logging
        var item = (await contentItemProvider.Get()
            .WhereEquals(nameof(ContentItemInfo.ContentItemID), contentItemId)
            .TopN(1)
            .Columns(
                nameof(ContentItemInfo.ContentItemGUID),
                nameof(ContentItemInfo.ContentItemName))
            .GetEnumerableTypedResultAsync())
            .FirstOrDefault();

        if (item is null)
        {
            return;
        }

        // All configured languages
        var languages = (await languageProvider.Get()
            .Columns(
                nameof(ContentLanguageInfo.ContentLanguageID),
                nameof(ContentLanguageInfo.ContentLanguageName))
            .GetEnumerableTypedResultAsync())
            .ToList();

        // Existing variants for this item
        var existingLanguageIds = (await metadataProvider.Get()
            .WhereEquals(nameof(ContentItemLanguageMetadataInfo.ContentItemLanguageMetadataContentItemID), contentItemId)
            .Columns(nameof(ContentItemLanguageMetadataInfo.ContentItemLanguageMetadataContentLanguageID))
            .GetEnumerableTypedResultAsync())
            .Select(m => m.ContentItemLanguageMetadataContentLanguageID)
            .ToHashSet();

        var missingLanguages = languages
            .Where(l => !existingLanguageIds.Contains(l.ContentLanguageID))
            .Select(l => l.ContentLanguageName)
            .ToList();

        if (missingLanguages.Count == 0)
        {
            return;
        }

        // Log missing translations
        _logger.LogWarning(
            "Content item '{Name}' ({Guid}) published but missing translations for: {Languages}",
            item.ContentItemName,
            item.ContentItemGUID,
            string.Join(", ", missingLanguages));

        // Invoke webhook callback if configured
        var webhookService = sp.GetService<ITranslationWebhookService>();
        if (webhookService is not null)
        {
            await webhookService.NotifyMissingTranslationsAsync(new MissingTranslationEvent
            {
                ContentItemId = contentItemId,
                ContentItemGuid = item.ContentItemGUID,
                ContentItemName = item.ContentItemName,
                MissingLanguages = missingLanguages,
                Timestamp = DateTimeOffset.UtcNow
            });
        }
    }
}
