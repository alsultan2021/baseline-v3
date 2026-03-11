namespace Baseline.Search;

/// <summary>
/// No-op implementation of <see cref="IMemberAuthorizationFilter"/>.
/// Allows all search results without filtering.
/// </summary>
public sealed class NoOpMemberAuthorizationFilter : IMemberAuthorizationFilter
{
    /// <inheritdoc />
    public Task<IEnumerable<SearchResult>> FilterResultsAsync(IEnumerable<SearchResult> results)
        => Task.FromResult(results);

    /// <inheritdoc />
    public Task<bool> CanAccessAsync(string documentId)
        => Task.FromResult(true);

    /// <inheritdoc />
    public Task<IEnumerable<string>> GetAuthorizedContentTypesAsync()
        => Task.FromResult<IEnumerable<string>>([]);

    /// <inheritdoc />
    public Task<IEnumerable<Guid>> GetAuthorizedTaxonomyTagsAsync()
        => Task.FromResult<IEnumerable<Guid>>([]);
}
