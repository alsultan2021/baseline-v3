using System.Text.Json;
using Baseline.Core;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Baseline.Navigation;

/// <summary>
/// Service for generating JSON-LD structured data for breadcrumbs.
/// Implements schema.org BreadcrumbList for SEO.
/// </summary>
public interface IBreadcrumbJsonLdService
{
    /// <summary>
    /// Generates JSON-LD script element for breadcrumbs.
    /// </summary>
    Task<IHtmlContent> GenerateJsonLdAsync();

    /// <summary>
    /// Generates JSON-LD script element for specific breadcrumb items.
    /// </summary>
    Task<IHtmlContent> GenerateJsonLdAsync(IEnumerable<BreadcrumbItem> breadcrumbs);

    /// <summary>
    /// Generates the raw JSON-LD object for breadcrumbs.
    /// </summary>
    Task<string> GenerateJsonAsync();

    /// <summary>
    /// Generates the raw JSON-LD object for specific breadcrumb items.
    /// </summary>
    Task<string> GenerateJsonAsync(IEnumerable<BreadcrumbItem> breadcrumbs);
}

/// <summary>
/// Implementation of breadcrumb JSON-LD service using schema.org BreadcrumbList.
/// </summary>
/// <remarks>
/// Generates structured data following Google's breadcrumb guidelines:
/// https://developers.google.com/search/docs/appearance/structured-data/breadcrumb
/// </remarks>
public sealed class BreadcrumbJsonLdService(
    IBreadcrumbService breadcrumbService,
    IHttpContextAccessor httpContextAccessor,
    IOptions<BaselineNavigationOptions> options) : IBreadcrumbJsonLdService
{
    private readonly BreadcrumbOptions _options = options.Value.Breadcrumbs;

    /// <inheritdoc />
    public async Task<IHtmlContent> GenerateJsonLdAsync()
    {
        if (!_options.GenerateStructuredData)
        {
            return HtmlString.Empty;
        }

        var breadcrumbs = await breadcrumbService.GetBreadcrumbsAsync();
        return await GenerateJsonLdAsync(breadcrumbs);
    }

    /// <inheritdoc />
    public Task<IHtmlContent> GenerateJsonLdAsync(IEnumerable<BreadcrumbItem> breadcrumbs)
    {
        var json = GenerateJson(breadcrumbs);

        if (string.IsNullOrEmpty(json))
        {
            return Task.FromResult<IHtmlContent>(HtmlString.Empty);
        }

        return Task.FromResult<IHtmlContent>(new HtmlString($"<script type=\"application/ld+json\">{json}</script>"));
    }

    /// <inheritdoc />
    public async Task<string> GenerateJsonAsync()
    {
        var breadcrumbs = await breadcrumbService.GetBreadcrumbsAsync();
        return GenerateJson(breadcrumbs);
    }

    /// <inheritdoc />
    public Task<string> GenerateJsonAsync(IEnumerable<BreadcrumbItem> breadcrumbs)
    {
        return Task.FromResult(GenerateJson(breadcrumbs));
    }

    private string GenerateJson(IEnumerable<BreadcrumbItem> breadcrumbs)
    {
        var items = breadcrumbs.ToList();

        if (items.Count == 0)
        {
            return string.Empty;
        }

        var baseUrl = GetBaseUrl();

        // Build the schema.org BreadcrumbList structure
        var breadcrumbList = new BreadcrumbListSchema
        {
            Context = "https://schema.org",
            Type = "BreadcrumbList",
            ItemListElement = items.Select((item, index) => new ListItemSchema
            {
                Type = "ListItem",
                Position = item.Position,
                Name = item.Name,
                Item = string.IsNullOrEmpty(item.Url)
                    ? null
                    : item.Url.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                        ? item.Url
                        : $"{baseUrl}{item.Url}"
            }).ToList()
        };

        return JsonSerializer.Serialize(breadcrumbList, JsonSerializerSettings.Default);
    }

    private string GetBaseUrl()
    {
        var request = httpContextAccessor.HttpContext?.Request;
        if (request is null)
        {
            return string.Empty;
        }

        return $"{request.Scheme}://{request.Host}";
    }

    /// <summary>
    /// JSON serializer settings for JSON-LD output.
    /// </summary>
    private static class JsonSerializerSettings
    {
        public static readonly JsonSerializerOptions Default = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }
}

/// <summary>
/// Schema.org BreadcrumbList structure.
/// </summary>
internal sealed class BreadcrumbListSchema
{
    [System.Text.Json.Serialization.JsonPropertyName("@context")]
    public required string Context { get; init; }

    [System.Text.Json.Serialization.JsonPropertyName("@type")]
    public required string Type { get; init; }

    public required List<ListItemSchema> ItemListElement { get; init; }
}

/// <summary>
/// Schema.org ListItem structure for breadcrumbs.
/// </summary>
internal sealed class ListItemSchema
{
    [System.Text.Json.Serialization.JsonPropertyName("@type")]
    public required string Type { get; init; }

    public int Position { get; init; }

    public string? Name { get; init; }

    public string? Item { get; init; }
}
