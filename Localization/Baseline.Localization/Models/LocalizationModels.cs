namespace Baseline.Localization;

/// <summary>
/// Represents a resource string with translations.
/// </summary>
public class ResourceString
{
    /// <summary>
    /// Unique key for the resource.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Default value (in default culture).
    /// </summary>
    public string DefaultValue { get; set; } = string.Empty;

    /// <summary>
    /// Translations by culture code.
    /// </summary>
    public Dictionary<string, string> Translations { get; set; } = [];

    /// <summary>
    /// Category/namespace for organization.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Description for translators.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Last modified date.
    /// </summary>
    public DateTimeOffset LastModified { get; set; }

    /// <summary>
    /// Gets the value for a specific culture.
    /// </summary>
    public string GetValue(string cultureCode)
    {
        return Translations.TryGetValue(cultureCode, out var value) 
            ? value 
            : DefaultValue;
    }
}

/// <summary>
/// Query parameters for resource strings.
/// </summary>
public class ResourceStringQuery
{
    /// <summary>
    /// Filter by key pattern.
    /// </summary>
    public string? KeyPattern { get; set; }

    /// <summary>
    /// Filter by category.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Filter by culture.
    /// </summary>
    public string? CultureCode { get; set; }

    /// <summary>
    /// Only show strings missing translation for this culture.
    /// </summary>
    public string? MissingTranslationCulture { get; set; }

    /// <summary>
    /// Page number.
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Page size.
    /// </summary>
    public int PageSize { get; set; } = 50;

    /// <summary>
    /// Sort field.
    /// </summary>
    public string SortBy { get; set; } = "Key";

    /// <summary>
    /// Sort descending.
    /// </summary>
    public bool SortDescending { get; set; }
}

/// <summary>
/// Result of a resource string operation.
/// </summary>
public record ResourceStringResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public ResourceString? ResourceString { get; init; }

    public static ResourceStringResult Succeeded(ResourceString? rs = null) => 
        new() { Success = true, ResourceString = rs };
    public static ResourceStringResult Failed(string message) => 
        new() { Success = false, ErrorMessage = message };
}

/// <summary>
/// Result of an import operation.
/// </summary>
public record ImportResult
{
    public bool Success { get; init; }
    public int ImportedCount { get; init; }
    public int UpdatedCount { get; init; }
    public int SkippedCount { get; init; }
    public IList<string> Errors { get; init; } = [];
}

/// <summary>
/// Paged result for lists.
/// </summary>
public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
}

/// <summary>
/// Alternate language URL for SEO.
/// </summary>
public class AlternateLanguageUrl
{
    /// <summary>
    /// Culture code.
    /// </summary>
    public string CultureCode { get; set; } = string.Empty;

    /// <summary>
    /// Hreflang value (e.g., "en-US", "x-default").
    /// </summary>
    public string Hreflang { get; set; } = string.Empty;

    /// <summary>
    /// URL for this culture.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Whether this is the default language.
    /// </summary>
    public bool IsDefault { get; set; }
}
