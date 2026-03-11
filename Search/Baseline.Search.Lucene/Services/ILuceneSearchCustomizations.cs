namespace Baseline.Search.Lucene;

/// <summary>
/// Interface for customizing Lucene search behavior.
/// </summary>
public interface ILuceneSearchCustomizations
{
    /// <summary>
    /// Customizes the search query before execution.
    /// </summary>
    Task<LuceneSearchQuery> CustomizeQueryAsync(
        LuceneSearchQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Customizes the search results after execution.
    /// </summary>
    Task<LuceneSearchResults> CustomizeResultsAsync(
        LuceneSearchResults results,
        LuceneSearchQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets custom boost values for fields.
    /// </summary>
    Dictionary<string, float> GetFieldBoosts();

    /// <summary>
    /// Gets custom analyzers for fields.
    /// </summary>
    Dictionary<string, string> GetFieldAnalyzers();
}

/// <summary>
/// Default implementation of Lucene search customizations.
/// </summary>
public sealed class DefaultLuceneSearchCustomizations : ILuceneSearchCustomizations
{
    public Task<LuceneSearchQuery> CustomizeQueryAsync(
        LuceneSearchQuery query,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(query);

    public Task<LuceneSearchResults> CustomizeResultsAsync(
        LuceneSearchResults results,
        LuceneSearchQuery query,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(results);

    public Dictionary<string, float> GetFieldBoosts() => new()
    {
        ["Title"] = 2.0f,
        ["Description"] = 1.5f,
        ["Content"] = 1.0f,
        ["Keywords"] = 1.8f
    };

    public Dictionary<string, string> GetFieldAnalyzers() => new()
    {
        ["Title"] = "standard",
        ["Description"] = "standard",
        ["Content"] = "standard"
    };
}
