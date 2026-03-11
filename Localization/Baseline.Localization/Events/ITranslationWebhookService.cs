using Microsoft.Extensions.Logging;

namespace Baseline.Localization.Events;

/// <summary>
/// Service that dispatches translation-missing notifications to external systems (webhooks, queues, etc.).
/// Register a custom implementation to integrate with your translation provider.
/// </summary>
public interface ITranslationWebhookService
{
    /// <summary>
    /// Called when a content item is published but missing translations in one or more languages.
    /// </summary>
    Task NotifyMissingTranslationsAsync(MissingTranslationEvent evt);
}

/// <summary>
/// Event payload for missing translation notifications.
/// </summary>
public sealed record MissingTranslationEvent
{
    public required int ContentItemId { get; init; }
    public required Guid ContentItemGuid { get; init; }
    public required string ContentItemName { get; init; }
    public required IReadOnlyList<string> MissingLanguages { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}

/// <summary>
/// Default no-op webhook service — logs only. Replace with your own implementation
/// to POST to external translation management systems.
/// </summary>
public sealed class LoggingTranslationWebhookService(
    Microsoft.Extensions.Logging.ILogger<LoggingTranslationWebhookService> logger) : ITranslationWebhookService
{
    public Task NotifyMissingTranslationsAsync(MissingTranslationEvent evt)
    {
        logger.LogInformation(
            "Translation webhook: '{Name}' ({Guid}) needs translation to [{Languages}]",
            evt.ContentItemName,
            evt.ContentItemGuid,
            string.Join(", ", evt.MissingLanguages));

        return Task.CompletedTask;
    }
}
