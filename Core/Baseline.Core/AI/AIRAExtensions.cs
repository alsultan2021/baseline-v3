using System.Collections.Concurrent;
using System.Reflection;
using System.Text.RegularExpressions;
using CMS.ContentEngine;
using CMS.Websites;
using Microsoft.Extensions.DependencyInjection;

namespace Baseline.Core.AI;

/// <summary>
/// AIRA (AI-Ready Architecture) integration helpers for Baseline v3.
/// Provides content structuring for AI agents and LLM consumption.
/// </summary>
public static class AIRAExtensions
{
    /// <summary>
    /// Adds AIRA integration services for AI-ready content delivery.
    /// </summary>
    public static IServiceCollection AddBaselineAIRA(
        this IServiceCollection services,
        Action<AIRAOptions>? configure = null)
    {
        var options = new AIRAOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddScoped<IAIContentProvider, AIContentProvider>();
        services.AddScoped<IContentStructurer, ContentStructurer>();

        return services;
    }
}

/// <summary>
/// AIRA configuration options.
/// </summary>
public class AIRAOptions
{
    /// <summary>
    /// Maximum content length for AI summaries. Default: 4000
    /// </summary>
    public int MaxSummaryLength { get; set; } = 4000;

    /// <summary>
    /// Include metadata in AI content. Default: true
    /// </summary>
    public bool IncludeMetadata { get; set; } = true;

    /// <summary>
    /// Content types to expose to AI. Empty = all types.
    /// </summary>
    public List<string> AllowedContentTypes { get; set; } = [];

    /// <summary>
    /// Fields to exclude from AI content.
    /// </summary>
    public List<string> ExcludedFields { get; set; } = ["Password", "ApiKey", "Secret"];

    /// <summary>
    /// Enable semantic chunking for long content. Default: true
    /// </summary>
    public bool EnableSemanticChunking { get; set; } = true;

    /// <summary>
    /// Chunk size for semantic chunking. Default: 1000
    /// </summary>
    public int ChunkSize { get; set; } = 1000;
}

/// <summary>
/// Interface for AI-ready content provider.
/// </summary>
public interface IAIContentProvider
{
    /// <summary>
    /// Get content item as AI-consumable structured data.
    /// </summary>
    Task<AIContent> GetContentForAIAsync<T>(T content) where T : class;

    /// <summary>
    /// Get content item as AI-consumable structured data with context.
    /// </summary>
    Task<AIContent> GetContentWithContextAsync<T>(T content, AIContentContext context) where T : class;

    /// <summary>
    /// Convert content to embedding-ready text.
    /// </summary>
    Task<string> GetEmbeddingTextAsync<T>(T content) where T : class;

    /// <summary>
    /// Get content chunks for RAG (Retrieval Augmented Generation).
    /// </summary>
    Task<IReadOnlyList<ContentChunk>> GetContentChunksAsync<T>(T content) where T : class;
}

/// <summary>
/// AI-ready content representation.
/// </summary>
public record AIContent
{
    /// <summary>
    /// Content type name.
    /// </summary>
    public string ContentType { get; init; } = "";

    /// <summary>
    /// Content item ID.
    /// </summary>
    public int ContentItemId { get; init; }

    /// <summary>
    /// Content item GUID.
    /// </summary>
    public Guid ContentItemGuid { get; init; }

    /// <summary>
    /// Display name or title.
    /// </summary>
    public string Title { get; init; } = "";

    /// <summary>
    /// Summary for AI consumption.
    /// </summary>
    public string Summary { get; init; } = "";

    /// <summary>
    /// Full content text.
    /// </summary>
    public string FullText { get; init; } = "";

    /// <summary>
    /// Structured fields as key-value pairs.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Fields { get; init; } = new Dictionary<string, object?>();

    /// <summary>
    /// Related content references.
    /// </summary>
    public IReadOnlyList<AIContentReference> RelatedContent { get; init; } = [];

    /// <summary>
    /// Content metadata.
    /// </summary>
    public AIContentMetadata Metadata { get; init; } = new();

    /// <summary>
    /// URL path if web page.
    /// </summary>
    public string? UrlPath { get; init; }

    /// <summary>
    /// Language code.
    /// </summary>
    public string Language { get; init; } = "en";
}

/// <summary>
/// AI content metadata.
/// </summary>
public record AIContentMetadata
{
    public DateTimeOffset? CreatedDate { get; init; }
    public DateTimeOffset? ModifiedDate { get; init; }
    public string? Author { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = [];
    public IReadOnlyList<string> Categories { get; init; } = [];
}

/// <summary>
/// Reference to related content.
/// </summary>
public record AIContentReference
{
    public string ContentType { get; init; } = "";
    public Guid ContentItemGuid { get; init; }
    public string Title { get; init; } = "";
    public string RelationshipType { get; init; } = "";
}

/// <summary>
/// Content chunk for RAG.
/// </summary>
public record ContentChunk
{
    public int Index { get; init; }
    public string Text { get; init; } = "";
    public int StartPosition { get; init; }
    public int EndPosition { get; init; }
    public string? Section { get; init; }
    public IReadOnlyDictionary<string, object?> Metadata { get; init; } = new Dictionary<string, object?>();
}

/// <summary>
/// Context for AI content generation.
/// </summary>
public record AIContentContext
{
    /// <summary>
    /// Include related content. Default: true
    /// </summary>
    public bool IncludeRelatedContent { get; init; } = true;

    /// <summary>
    /// Maximum depth for related content. Default: 1
    /// </summary>
    public int RelatedContentDepth { get; init; } = 1;

    /// <summary>
    /// Include HTML content. Default: false
    /// </summary>
    public bool IncludeHtml { get; init; }

    /// <summary>
    /// Specific fields to include. Empty = all fields.
    /// </summary>
    public List<string> IncludeFields { get; init; } = [];

    /// <summary>
    /// Fields to exclude.
    /// </summary>
    public List<string> ExcludeFields { get; init; } = [];
}

/// <summary>
/// Default implementation of IAIContentProvider.
/// </summary>
public class AIContentProvider : IAIContentProvider
{
    // Cache property lists per type and individual property lookups
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _propsCache = new();
    private static readonly ConcurrentDictionary<(Type, string), PropertyInfo?> _propCache = new();

    private readonly AIRAOptions _options;
    private readonly IContentStructurer _structurer;

    public AIContentProvider(AIRAOptions options, IContentStructurer structurer)
    {
        _options = options;
        _structurer = structurer;
    }

    public async Task<AIContent> GetContentForAIAsync<T>(T content) where T : class
    {
        return await GetContentWithContextAsync(content, new AIContentContext());
    }

    public async Task<AIContent> GetContentWithContextAsync<T>(T content, AIContentContext context) where T : class
    {
        var type = content.GetType();
        var fields = new Dictionary<string, object?>();

        // Extract properties (cached per type)
        foreach (var prop in _propsCache.GetOrAdd(type, static t => t.GetProperties()))
        {
            if (_options.ExcludedFields.Contains(prop.Name))
                continue;

            if (context.ExcludeFields.Contains(prop.Name))
                continue;

            if (context.IncludeFields.Count > 0 && !context.IncludeFields.Contains(prop.Name))
                continue;

            var value = prop.GetValue(content);
            fields[prop.Name] = value;
        }

        // Get title and summary
        var title = GetPropertyValue<string>(content, "Title") ??
                    GetPropertyValue<string>(content, "Name") ??
                    GetPropertyValue<string>(content, "DisplayName") ?? "";

        var description = GetPropertyValue<string>(content, "Description") ??
                          GetPropertyValue<string>(content, "Summary") ??
                          GetPropertyValue<string>(content, "MetaData_Description") ?? "";

        // Get full text content
        var fullText = await _structurer.ExtractTextAsync(content);

        // Get URL if web page
        string? urlPath = null;
        if (content is IWebPageFieldsSource webPage)
        {
            urlPath = webPage.SystemFields.WebPageUrlPath;
        }

        // Build metadata
        var metadata = new AIContentMetadata
        {
            CreatedDate = GetPropertyValue<DateTimeOffset?>(content, "CreatedDate") ??
                          GetPropertyValue<DateTimeOffset?>(content, "SystemFields.ContentItemCommonDataContentItemCreatedWhen"),
            ModifiedDate = GetPropertyValue<DateTimeOffset?>(content, "ModifiedDate") ??
                           GetPropertyValue<DateTimeOffset?>(content, "SystemFields.ContentItemCommonDataContentItemModifiedWhen"),
            Author = GetPropertyValue<string>(content, "Author"),
            Tags = GetTags(content),
            Categories = GetCategories(content)
        };

        return await Task.FromResult(new AIContent
        {
            ContentType = type.Name,
            Title = title,
            Summary = TruncateText(description, _options.MaxSummaryLength),
            FullText = fullText,
            Fields = fields,
            UrlPath = urlPath,
            Metadata = metadata
        });
    }

    public async Task<string> GetEmbeddingTextAsync<T>(T content) where T : class
    {
        var aiContent = await GetContentForAIAsync(content);

        // Combine title, summary, and key fields for embedding
        var parts = new List<string> { aiContent.Title };

        if (!string.IsNullOrEmpty(aiContent.Summary))
            parts.Add(aiContent.Summary);

        parts.Add(aiContent.FullText);

        return string.Join("\n\n", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
    }

    public async Task<IReadOnlyList<ContentChunk>> GetContentChunksAsync<T>(T content) where T : class
    {
        var fullText = await _structurer.ExtractTextAsync(content);

        if (!_options.EnableSemanticChunking)
        {
            return
            [
                new() { Index = 0, Text = fullText, StartPosition = 0, EndPosition = fullText.Length }
            ];
        }

        // Semantic chunking by paragraphs/sections
        var chunks = new List<ContentChunk>();
        var paragraphs = fullText.Split(["\n\n", "\r\n\r\n"], StringSplitOptions.RemoveEmptyEntries);

        var currentChunk = new List<string>();
        var currentLength = 0;
        var position = 0;
        var chunkIndex = 0;

        foreach (var paragraph in paragraphs)
        {
            if (currentLength + paragraph.Length > _options.ChunkSize && currentChunk.Count > 0)
            {
                // Save current chunk
                var chunkText = string.Join("\n\n", currentChunk);
                chunks.Add(new ContentChunk
                {
                    Index = chunkIndex++,
                    Text = chunkText,
                    StartPosition = position - chunkText.Length,
                    EndPosition = position
                });

                currentChunk.Clear();
                currentLength = 0;
            }

            currentChunk.Add(paragraph);
            currentLength += paragraph.Length;
            position += paragraph.Length + 2; // +2 for \n\n
        }

        // Add remaining chunk
        if (currentChunk.Count > 0)
        {
            var chunkText = string.Join("\n\n", currentChunk);
            chunks.Add(new ContentChunk
            {
                Index = chunkIndex,
                Text = chunkText,
                StartPosition = position - chunkText.Length,
                EndPosition = position
            });
        }

        return chunks;
    }

    private static T? GetPropertyValue<T>(object obj, string propertyName)
    {
        var prop = _propCache.GetOrAdd((obj.GetType(), propertyName), static key =>
            key.Item1.GetProperty(key.Item2));
        if (prop == null) return default;

        var value = prop.GetValue(obj);
        if (value is T typedValue) return typedValue;

        return default;
    }

    private static IReadOnlyList<string> GetTags(object content)
    {
        var tags = GetPropertyValue<object>(content, "Tags");
        if (tags is IEnumerable<string> stringTags)
            return stringTags.ToList();
        return [];
    }

    private static IReadOnlyList<string> GetCategories(object content)
    {
        var categories = GetPropertyValue<object>(content, "Categories");
        if (categories is IEnumerable<string> stringCategories)
            return stringCategories.ToList();
        return [];
    }

    private static string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;

        return text[..maxLength] + "...";
    }
}

/// <summary>
/// Interface for content text extraction.
/// </summary>
public interface IContentStructurer
{
    /// <summary>
    /// Extract plain text from content item.
    /// </summary>
    Task<string> ExtractTextAsync<T>(T content) where T : class;
}

/// <summary>
/// Default content structurer implementation.
/// </summary>
public partial class ContentStructurer : IContentStructurer
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _propsCache = new();

    [GeneratedRegex(@"<[^>]*>")]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex MultiWhitespaceRegex();

    public Task<string> ExtractTextAsync<T>(T content) where T : class
    {
        var type = content.GetType();
        var textParts = new List<string>();

        // Extract text from string properties (cached per type)
        foreach (var prop in _propsCache.GetOrAdd(type, static t => t.GetProperties()))
        {
            if (prop.PropertyType != typeof(string))
                continue;

            var value = prop.GetValue(content) as string;
            if (string.IsNullOrWhiteSpace(value))
                continue;

            // Skip system fields and URLs
            if (prop.Name.Contains("Url") || prop.Name.Contains("Path") ||
                prop.Name.Contains("Guid") || prop.Name.Contains("Id"))
                continue;

            // Strip HTML if present
            var text = StripHtml(value);
            if (!string.IsNullOrWhiteSpace(text))
            {
                textParts.Add(text);
            }
        }

        return Task.FromResult(string.Join("\n\n", textParts));
    }

    private static string StripHtml(string html)
    {
        if (string.IsNullOrEmpty(html))
            return html;

        var text = HtmlTagRegex().Replace(html, " ");
        text = MultiWhitespaceRegex().Replace(text, " ");
        return text.Trim();
    }
}
