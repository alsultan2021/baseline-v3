using Baseline.Search;

namespace Search.Models;

/// <summary>
/// Search model - alias for v3 SearchRequest
/// </summary>
public class Search : SearchRequest { }

/// <summary>
/// SearchResult model
/// </summary>
public class SearchResult
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Url { get; set; } = string.Empty;
    public double Score { get; set; }
}
