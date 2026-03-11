namespace Search.Features.Search;

/// <summary>
/// Search results wrapper that supports TryGetValue pattern
/// </summary>
public readonly struct SearchResultsMaybe
{
    private readonly SearchResultsData? _results;

    public SearchResultsMaybe(SearchResultsData? results) => _results = results;

    public bool TryGetValue(out SearchResultsData results)
    {
        if (_results != null)
        {
            results = _results;
            return true;
        }
        results = new SearchResultsData();
        return false;
    }

    public bool HasValue => _results != null;
}

/// <summary>
/// Search results with TotalPossible property
/// </summary>
public class SearchResultsData
{
    public IEnumerable<SearchResultItem> Items { get; set; } = [];
    public int TotalPossible { get; set; }
}
