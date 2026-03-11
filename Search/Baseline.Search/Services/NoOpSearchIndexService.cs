namespace Baseline.Search;

/// <summary>
/// No-op implementation of <see cref="ISearchIndexService"/>.
/// Used as a default when no concrete provider (Lucene, Algolia, Azure) is registered.
/// </summary>
public sealed class NoOpSearchIndexService : ISearchIndexService
{
    /// <inheritdoc />
    public Task IndexAsync(SearchDocument document) => Task.CompletedTask;

    /// <inheritdoc />
    public Task IndexBatchAsync(IEnumerable<SearchDocument> documents) => Task.CompletedTask;

    /// <inheritdoc />
    public Task RemoveAsync(string documentId) => Task.CompletedTask;

    /// <inheritdoc />
    public Task RebuildIndexAsync(IProgress<IndexRebuildProgress>? progress = null) => Task.CompletedTask;

    /// <inheritdoc />
    public Task<IndexStatistics> GetStatisticsAsync() => Task.FromResult(new IndexStatistics());

    /// <inheritdoc />
    public Task ClearIndexAsync() => Task.CompletedTask;
}
