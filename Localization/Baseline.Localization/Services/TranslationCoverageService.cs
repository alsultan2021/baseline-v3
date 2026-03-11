using CMS.ContentEngine;
using CMS.ContentEngine.Internal;
using CMS.DataEngine;

using Baseline.Localization.Infrastructure;

using Microsoft.Extensions.Logging;

namespace Baseline.Localization.Services;

/// <summary>
/// Service that computes translation coverage statistics across all languages
/// and persists snapshots to the DB for admin listing display.
/// </summary>
public interface ITranslationCoverageService
{
    /// <summary>
    /// Recomputes coverage for all languages and stores results.
    /// </summary>
    Task RefreshCoverageAsync();
}

public sealed class TranslationCoverageService(
    IInfoProvider<ContentLanguageInfo> languageProvider,
    IInfoProvider<ContentItemLanguageMetadataInfo> metadataProvider,
    IInfoProvider<ContentItemInfo> contentItemProvider,
    ILogger<TranslationCoverageService> logger) : ITranslationCoverageService
{
    // Lazy provider — table may not exist at DI validation time
    private static IInfoProvider<TranslationCoverageSnapshotInfo> SnapshotProvider
        => Provider<TranslationCoverageSnapshotInfo>.Instance;

    public async Task RefreshCoverageAsync()
    {
        logger.LogDebug("Refreshing translation coverage snapshots");

        // Get all languages
        var languages = (await languageProvider.Get()
            .Columns(
                nameof(ContentLanguageInfo.ContentLanguageID),
                nameof(ContentLanguageInfo.ContentLanguageName),
                nameof(ContentLanguageInfo.ContentLanguageDisplayName),
                nameof(ContentLanguageInfo.ContentLanguageIsDefault))
            .GetEnumerableTypedResultAsync())
            .ToList();

        if (languages.Count == 0)
        {
            return;
        }

        // Total distinct content items
        var totalItemCount = (await contentItemProvider.Get()
            .Columns(nameof(ContentItemInfo.ContentItemID))
            .GetEnumerableTypedResultAsync())
            .Count();

        if (totalItemCount == 0)
        {
            return;
        }

        // Get translated item count per language
        var metadata = (await metadataProvider.Get()
            .Columns(
                nameof(ContentItemLanguageMetadataInfo.ContentItemLanguageMetadataContentItemID),
                nameof(ContentItemLanguageMetadataInfo.ContentItemLanguageMetadataContentLanguageID))
            .GetEnumerableTypedResultAsync())
            .ToList();

        var countsByLanguageId = metadata
            .GroupBy(m => m.ContentItemLanguageMetadataContentLanguageID)
            .ToDictionary(g => g.Key, g => g.Select(m => m.ContentItemLanguageMetadataContentItemID).Distinct().Count());

        // Clear old snapshots
        var oldSnapshots = await SnapshotProvider.Get().GetEnumerableTypedResultAsync();
        foreach (var old in oldSnapshots)
        {
            SnapshotProvider.Delete(old);
        }

        var now = DateTime.UtcNow;

        // Create new snapshots
        foreach (var lang in languages)
        {
            countsByLanguageId.TryGetValue(lang.ContentLanguageID, out var translated);
            var pct = totalItemCount > 0 ? (int)Math.Round(100.0 * translated / totalItemCount) : 0;

            var snapshot = new TranslationCoverageSnapshotInfo
            {
                LanguageCode = lang.ContentLanguageName,
                LanguageDisplayName = lang.ContentLanguageDisplayName,
                TotalContentItems = totalItemCount,
                TranslatedItems = translated,
                CoveragePercent = pct,
                IsDefault = lang.ContentLanguageIsDefault,
                ComputedAtUtc = now
            };

            SnapshotProvider.Set(snapshot);
        }

        logger.LogDebug("Translation coverage refreshed: {Count} languages, {Total} total items", languages.Count, totalItemCount);
    }
}
