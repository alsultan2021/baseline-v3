using CSharpFunctionalExtensions;

namespace Search.Features.Search;

/// <summary>
/// Search result item
/// </summary>
public class SearchResultItem
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? ContentType { get; set; }
    public DateTime? Date { get; set; }
    public double Score { get; set; }
    public string? Highlight { get; set; }

    // Additional properties used by chevalroyal views
    public bool IsPage { get; set; }
    public Maybe<string> PageUrl { get; set; }
    public string? Content { get; set; }
}
