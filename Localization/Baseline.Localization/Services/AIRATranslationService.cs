using CMS.ContentEngine;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baseline.Localization.Services;

/// <summary>
/// Service for integrating with XbK AIRA (AI-Assisted) translation features.
/// Provides programmatic access to machine translation and translation workflow.
/// 
/// <para>
/// <b>Important:</b> Kentico AIRA translation is currently an admin-UI-only feature.
/// There is no public programmatic API for triggering translations from code.
/// This service provides a ready-to-wire abstraction that:
/// <list type="bullet">
///   <item>Detects AIRA availability via license/options configuration</item>
///   <item>Returns clear failure messages when AIRA is not available</item>
///   <item>Can be replaced with a real implementation when/if Kentico exposes a public API</item>
/// </list>
/// </para>
/// 
/// Requires Xperience by Kentico Advanced license tier.
/// See: https://docs.kentico.com/documentation/business-users/aira
/// </summary>
public interface IAIRATranslationService
{
    /// <summary>
    /// Checks if AIRA translation is available.
    /// </summary>
    Task<bool> IsAvailableAsync();

    /// <summary>
    /// Gets translation status for a content item.
    /// </summary>
    Task<TranslationStatus> GetTranslationStatusAsync(Guid contentItemGuid, string targetLanguage);

    /// <summary>
    /// Queues a content item for AIRA translation.
    /// </summary>
    Task<TranslationQueueResult> QueueForTranslationAsync(
        Guid contentItemGuid,
        string targetLanguage,
        TranslationOptions? options = null);

    /// <summary>
    /// Queues multiple content items for batch translation.
    /// </summary>
    Task<TranslationQueueResult> QueueBatchTranslationAsync(
        IEnumerable<Guid> contentItemGuids,
        string targetLanguage,
        TranslationOptions? options = null);

    /// <summary>
    /// Gets the translation queue status.
    /// </summary>
    Task<TranslationQueueStatus> GetQueueStatusAsync();

    /// <summary>
    /// Gets content items pending translation.
    /// </summary>
    Task<IEnumerable<PendingTranslation>> GetPendingTranslationsAsync(string? targetLanguage = null);

    /// <summary>
    /// Cancels a pending translation.
    /// </summary>
    Task<bool> CancelTranslationAsync(Guid translationId);

    /// <summary>
    /// Gets translation history for a content item.
    /// </summary>
    Task<IEnumerable<TranslationHistoryEntry>> GetTranslationHistoryAsync(Guid contentItemGuid);
}

/// <summary>
/// Options for translation operations.
/// </summary>
public class TranslationOptions
{
    /// <summary>
    /// Include linked content items in translation.
    /// Default: true
    /// </summary>
    public bool IncludeLinkedItems { get; set; } = true;

    /// <summary>
    /// Maximum depth for linked item translation.
    /// Default: 1
    /// </summary>
    public int LinkedItemsDepth { get; set; } = 1;

    /// <summary>
    /// Custom translation prompt to guide AIRA.
    /// </summary>
    public string? CustomPrompt { get; set; }

    /// <summary>
    /// Preserve specific terms without translation.
    /// </summary>
    public List<string> PreserveTerms { get; set; } = [];

    /// <summary>
    /// Auto-publish after translation completes.
    /// Default: false
    /// </summary>
    public bool AutoPublish { get; set; }

    /// <summary>
    /// Notify users when translation completes.
    /// </summary>
    public List<int> NotifyUserIds { get; set; } = [];
}

/// <summary>
/// Translation status for a content item.
/// </summary>
public record TranslationStatus
{
    public required Guid ContentItemGuid { get; init; }
    public required string TargetLanguage { get; init; }
    public required TranslationState State { get; init; }
    public DateTimeOffset? QueuedAt { get; init; }
    public DateTimeOffset? StartedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public string? ErrorMessage { get; init; }
    public int? ProgressPercent { get; init; }
}

/// <summary>
/// Translation state enum.
/// </summary>
public enum TranslationState
{
    /// <summary>
    /// No translation exists.
    /// </summary>
    NotTranslated,

    /// <summary>
    /// Translation is queued for processing.
    /// </summary>
    Queued,

    /// <summary>
    /// Translation is in progress.
    /// </summary>
    InProgress,

    /// <summary>
    /// Translation completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Translation failed.
    /// </summary>
    Failed,

    /// <summary>
    /// Translation was cancelled.
    /// </summary>
    Cancelled
}

/// <summary>
/// Result of queuing for translation.
/// </summary>
public record TranslationQueueResult
{
    public bool Success { get; init; }
    public Guid? TranslationId { get; init; }
    public string? ErrorMessage { get; init; }
    public int QueuedCount { get; init; }
    public int SkippedCount { get; init; }
    public IList<string> SkippedReasons { get; init; } = [];

    public static TranslationQueueResult Succeeded(Guid translationId, int count = 1) =>
        new() { Success = true, TranslationId = translationId, QueuedCount = count };

    public static TranslationQueueResult Failed(string message) =>
        new() { Success = false, ErrorMessage = message };
}

/// <summary>
/// Overall translation queue status.
/// </summary>
public record TranslationQueueStatus
{
    public int PendingCount { get; init; }
    public int InProgressCount { get; init; }
    public int CompletedTodayCount { get; init; }
    public int FailedTodayCount { get; init; }
    public DateTimeOffset? OldestQueuedAt { get; init; }
}

/// <summary>
/// Pending translation entry.
/// </summary>
public record PendingTranslation
{
    public required Guid TranslationId { get; init; }
    public required Guid ContentItemGuid { get; init; }
    public required string ContentItemName { get; init; }
    public required string SourceLanguage { get; init; }
    public required string TargetLanguage { get; init; }
    public required TranslationState State { get; init; }
    public DateTimeOffset QueuedAt { get; init; }
    public int? ProgressPercent { get; init; }
}

/// <summary>
/// Translation history entry.
/// </summary>
public record TranslationHistoryEntry
{
    public required Guid TranslationId { get; init; }
    public required string SourceLanguage { get; init; }
    public required string TargetLanguage { get; init; }
    public required TranslationState State { get; init; }
    public DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public string? TranslatedBy { get; init; }
    public int? WordCount { get; init; }
}

/// <summary>
/// Default implementation of AIRA translation service.
/// <para>
/// Kentico AIRA translation is admin-UI-only — no public programmatic API exists.
/// This implementation checks configuration and returns appropriate status messages.
/// Replace this class with a real implementation when Kentico provides a public API.
/// </para>
/// </summary>
public sealed class AIRATranslationService(
    ILogger<AIRATranslationService> logger,
    IOptions<BaselineLocalizationOptions> options) : IAIRATranslationService
{
    private const string NotAvailableMessage =
        "AIRA translation is an admin-UI feature. Use the Xperience admin to translate content, " +
        "or enable EnableAIRATranslation in options once a public API is available.";

    public Task<bool> IsAvailableAsync()
    {
        var isEnabled = options.Value.EnableAIRATranslation;
        logger.LogDebug("AIRA translation availability check: {Enabled}", isEnabled);
        return Task.FromResult(isEnabled);
    }

    public Task<TranslationStatus> GetTranslationStatusAsync(Guid contentItemGuid, string targetLanguage)
    {
        // Query the translation queue for status
        return Task.FromResult(new TranslationStatus
        {
            ContentItemGuid = contentItemGuid,
            TargetLanguage = targetLanguage,
            State = TranslationState.NotTranslated
        });
    }

    public Task<TranslationQueueResult> QueueForTranslationAsync(
        Guid contentItemGuid,
        string targetLanguage,
        TranslationOptions? options = null)
    {
        logger.LogInformation(
            "Queueing content item {ContentItemGuid} for translation to {TargetLanguage}",
            contentItemGuid, targetLanguage);

        // AIRA has no public programmatic API — translate via the admin UI
        return Task.FromResult(TranslationQueueResult.Failed(NotAvailableMessage));
    }

    public Task<TranslationQueueResult> QueueBatchTranslationAsync(
        IEnumerable<Guid> contentItemGuids,
        string targetLanguage,
        TranslationOptions? options = null)
    {
        var count = contentItemGuids.Count();
        logger.LogInformation(
            "Queueing {Count} content items for batch translation to {TargetLanguage}",
            count, targetLanguage);

        return Task.FromResult(TranslationQueueResult.Failed(NotAvailableMessage));
    }

    public Task<TranslationQueueStatus> GetQueueStatusAsync()
    {
        return Task.FromResult(new TranslationQueueStatus
        {
            PendingCount = 0,
            InProgressCount = 0,
            CompletedTodayCount = 0,
            FailedTodayCount = 0
        });
    }

    public Task<IEnumerable<PendingTranslation>> GetPendingTranslationsAsync(string? targetLanguage = null)
    {
        return Task.FromResult<IEnumerable<PendingTranslation>>([]);
    }

    public Task<bool> CancelTranslationAsync(Guid translationId)
    {
        logger.LogWarning("Attempted to cancel translation {TranslationId} but AIRA is not configured", translationId);
        return Task.FromResult(false);
    }

    public Task<IEnumerable<TranslationHistoryEntry>> GetTranslationHistoryAsync(Guid contentItemGuid)
    {
        return Task.FromResult<IEnumerable<TranslationHistoryEntry>>([]);
    }
}

/// <summary>
/// Helpers for translation workflow integration.
/// </summary>
public static class TranslationWorkflowExtensions
{
    /// <summary>
    /// Checks if a content item needs translation based on source changes.
    /// </summary>
    public static async Task<bool> NeedsRetranslationAsync(
        this IAIRATranslationService service,
        Guid contentItemGuid,
        string targetLanguage,
        DateTimeOffset sourceModifiedSince)
    {
        var status = await service.GetTranslationStatusAsync(contentItemGuid, targetLanguage);

        if (status.State == TranslationState.NotTranslated)
        {
            return true;
        }

        // If source was modified after last translation, needs retranslation
        return status.CompletedAt.HasValue && sourceModifiedSince > status.CompletedAt.Value;
    }
}
