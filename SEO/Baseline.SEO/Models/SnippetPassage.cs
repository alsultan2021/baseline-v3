namespace Baseline.SEO;

/// <summary>
/// A passage optimized for featured snippets.
/// </summary>
public record SnippetPassage
{
    /// <summary>
    /// The question being answered.
    /// </summary>
    public required string Question { get; init; }

    /// <summary>
    /// The optimized passage.
    /// </summary>
    public required string Passage { get; init; }

    /// <summary>
    /// Type of snippet this targets.
    /// </summary>
    public SnippetType Type { get; init; }

    /// <summary>
    /// Word count (optimal: 40-60 for paragraph snippets).
    /// </summary>
    public int WordCount { get; init; }

    /// <summary>
    /// Estimated snippet probability.
    /// </summary>
    public double Probability { get; init; }

    /// <summary>
    /// For list snippets: the list items.
    /// </summary>
    public IReadOnlyList<string>? ListItems { get; init; }

    /// <summary>
    /// For table snippets: the table data.
    /// </summary>
    public TableData? Table { get; init; }
}

/// <summary>
/// Table data for table snippets.
/// </summary>
public record TableData
{
    public IReadOnlyList<string> Headers { get; init; } = [];
    public IReadOnlyList<IReadOnlyList<string>> Rows { get; init; } = [];
}
