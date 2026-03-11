namespace Search.Features.Search;

/// <summary>
/// Search view model
/// </summary>
public class SearchViewModel
{
    public string Query { get; set; } = string.Empty;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalResults { get; set; }
    public int TotalPages { get; set; }
    public IEnumerable<SearchResultItem> Results { get; set; } = [];
    public string? ErrorMessage { get; set; }

    // Additional properties used by chevalroyal views
    public string? SearchValue { get; set; }
    public SearchResultsMaybe SearchResults { get; set; }
}
